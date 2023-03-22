﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using TranspileUtilities;
using UnityEngine;

namespace PriorityIngredients
{
    [HarmonyPatch(typeof(BlueprintConstructionPopup), "AutoFill")]
    public static class BlueprintConstructionPopup_AutoFill_Patch
    {
        private static List<CardPriority> _priorityCards;

        /// <summary>
        /// Loads the cards from the Plugin settings.
        /// </summary>
        public static void LoadCardPrioity()
        {
            List<string> cardNames = Plugin.CardPriorityList.Value.Split(',')
                .Select(x => x.Trim())
                .ToList();

            int sequence = 1;

            _priorityCards = cardNames
                .Select(x => new CardPriority(sequence++, x))
                .ToList();
        }

        static BlueprintConstructionPopup_AutoFill_Patch()
        {
            LoadCardPrioity();
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {

            MethodInfo getAllCardsInfo = typeof(BlueprintElement)
                    .GetProperty(nameof(BlueprintElement.AllCards))
                    .GetGetMethod();


            List<CodeInstruction> existingInstructions = instructions.ToList();
            


            //Looking for:
            // List<CardData> allCards = blueprintStage.RequiredElements[i].AllCards;

            StackVariableInstruction cardListVariable = null;   //The stack allocated card list variable.

            CodeMatcher codeMatcher = new CodeMatcher(existingInstructions);

            codeMatcher
                .MatchForward(true,
                    new CodeMatch(x => x.IsLdloc()),
                    new CodeMatch(OpCodes.Ldelema, typeof(BlueprintElement)),
                    new CodeMatch(OpCodes.Call, getAllCardsInfo),
                    new CodeMatch(instruction => StackVariableInstruction.Create(true, instruction, out cardListVariable))
                   )
                .ThrowIfNotMatch($"Could not find GetAllCards section")
                .Advance(1)

                //Call OrderByPriority
                .Insert(
                    cardListVariable.Load,
                    CodeInstruction.Call(typeof(BlueprintConstructionPopup_AutoFill_Patch),
                        nameof(BlueprintConstructionPopup_AutoFill_Patch.OrderByPriority), new Type[] { typeof(List<CardData>) }),
                    cardListVariable.Store
                )
                .InstructionEnumeration()
                .ToList();

            var newInstructions = codeMatcher.InstructionEnumeration() .ToList();  
            return newInstructions;
        }

        /// <summary>
        /// Given a list of ingredient cards, reorders the source card list
        /// so the priority ingredients are at the start of the list, in order
        /// of the provided priority list.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static List<CardData> OrderByPriority(List<CardData> source)
        {

            //Get the cards that match the priority ingredients, and order by the priority list source's order.
            List<CardData> priorityCards = _priorityCards.Join(source, inner => inner.CardName, outer => outer.name, 
                    (inner, outer) => new { inner, outer })
                .OrderBy(x => x.inner.Sequence)
                .Select(x => x.outer)
                .ToList();

            List<CardData> newCardList = source
                .Where(x => !priorityCards.Contains(x))
                .ToList();

            newCardList.InsertRange(0, priorityCards);

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
