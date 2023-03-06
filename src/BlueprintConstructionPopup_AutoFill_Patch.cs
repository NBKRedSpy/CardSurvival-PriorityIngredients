using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;

namespace PriorityIngredients
{
    [HarmonyPatch(typeof(BlueprintConstructionPopup), "AutoFill")]
    public static class BlueprintConstructionPopup_AutoFill_Patch
    {
        private static readonly List<CardPriority> _priorityCards;

        static BlueprintConstructionPopup_AutoFill_Patch()
        {
            List<string> cardNames = Plugin.CardPriorityList.Split(',')
                .Select(x=> x.Trim())
                .ToList();

            int sequence = 1;

            _priorityCards = cardNames
                .Select(x => new CardPriority(sequence++, x))
                .ToList();
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {

            MethodInfo getAllCardsInfo = typeof(BlueprintElement)
                    .GetProperty(nameof(BlueprintElement.AllCards))
                    .GetGetMethod();


            List<CodeInstruction> existingInstructions = instructions.ToList();

            //Looking for:
            // List<CardData> allCards = blueprintStage.RequiredElements[i].AllCards;
            List<CodeInstruction> newInstructions = new CodeMatcher(existingInstructions)
                .MatchForward(true,
                    new CodeMatch(OpCodes.Ldloc_S),
                    new CodeMatch(OpCodes.Ldelema, typeof(BlueprintElement)),
                    new CodeMatch(OpCodes.Call, getAllCardsInfo),
                    new CodeMatch(OpCodes.Stloc_3)
                   )
                .ThrowIfNotMatch($"Could not find GetAllCards section")
                .Advance(1)

                //Call OrderByPriority
                .Insert(
                    new CodeInstruction(OpCodes.Ldloc_3),
                    CodeInstruction.Call(typeof(BlueprintConstructionPopup_AutoFill_Patch),
                        nameof(BlueprintConstructionPopup_AutoFill_Patch.OrderByPriority), new Type[] { typeof(List<CardData>) }),
                    new CodeInstruction(OpCodes.Stloc_3)
                )
                .InstructionEnumeration()
                .ToList();

            return newInstructions;
        }

        /// <summary>
        /// Given a list of ingredient cards, reorders the source card list
        /// so the priority ingredients are at the start of the list, in order
        /// of the prvided priority list.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static List<CardData> OrderByPriority(List<CardData> source)
        {

            //Get the cards that match the priority ingredients, and order by the priority list source's order.
            var selectedItems = _priorityCards.Join(source, inner => inner.CardName, outer => outer.name, 
                    (inner, outer) => new { inner, outer })
                .OrderBy(x => x.inner.Sequence)
                .Select(x => x.outer)
                .ToList();

            var newCardList = source
                .Where(x => !selectedItems.Contains(x))
                .ToList();

            newCardList.InsertRange(0, selectedItems);

            return newCardList;
        }

        private class CardPriority
        {
            public int Sequence { get; set; }
            public string CardName { get; set; }

            public CardPriority(int sequence, string cardName)
            {
                Sequence = sequence;
                CardName = cardName;
            }
        }

    }
}
