using PrepareLanding.Core.Extensions;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace PrepareLanding.Core.Gui
{
    [StaticConstructorOnStartup]
    public static class Widgets
    {
        public static readonly Texture2D Minus = ContentFinder<Texture2D>.Get("UI/Buttons/Minus");
        public static readonly Texture2D CloseXSmall = ContentFinder<Texture2D>.Get("UI/Widgets/CloseXSmall");

        public static Color InactiveColor = new Color(0.37f, 0.37f, 0.37f, 0.8f);

        public static void CheckBoxLabeledMulti(Rect rect, string label, ref MultiCheckboxState state,
            bool disabled = false)
        {
            var anchor = Text.Anchor;
            Text.Anchor = TextAnchor.MiddleLeft;
            Verse.Widgets.Label(rect, label);

            if (!disabled && Verse.Widgets.ButtonInvisible(rect))
                switch (state)
                {
                    case MultiCheckboxState.On:
                        state = MultiCheckboxState.Partial;
                        SoundDefOf.CheckboxTurnedOn.PlayOneShotOnCamera();
                        break;

                    case MultiCheckboxState.Partial:
                        state = MultiCheckboxState.Off;
                        SoundDefOf.CheckboxTurnedOff.PlayOneShotOnCamera();
                        break;

                    case MultiCheckboxState.Off:
                        state = MultiCheckboxState.On;
                        SoundDefOf.CheckboxTurnedOn.PlayOneShotOnCamera();
                        break;
                }

            var vector2 = Vector2.zero;
            vector2.x = rect.x + rect.width - 24f;
            vector2.y = rect.y;
            Verse.Widgets.CheckboxMulti(vector2, state, 24f);

            Text.Anchor = anchor;
        }

        public static void CheckBoxMultiDraw(float x, float y, MultiCheckboxState state, bool disabled,
            float size = 24f)
        {
            var color = GUI.color;
            if (disabled)
                GUI.color = InactiveColor;

            Texture2D image;
            switch (state)
            {
                case MultiCheckboxState.On:
                    image = Verse.Widgets.CheckboxOnTex;
                    break;

                case MultiCheckboxState.Partial:
                    image = Verse.Widgets.CheckboxPartialTex;
                    break;

                case MultiCheckboxState.Off:
                    image = Verse.Widgets.CheckboxOffTex;
                    break;

                default:
                    image = Verse.Widgets.CheckboxOffTex;
                    break;
            }

            var position = new Rect(x, y, size, size);
            GUI.DrawTexture(position, image);
            if (disabled)
                GUI.color = color;
        }

        public static bool CheckBoxLabeledSelectableMulti(Rect rect, string label, ref bool selected,
            ref MultiCheckboxState state, bool disabled = false)
        {
            if (selected)
                Verse.Widgets.DrawHighlight(rect);
            Verse.Widgets.Label(rect, label);
            var flag = selected;
            var butRect = rect;
            butRect.width -= 24f;
            if (!selected && Verse.Widgets.ButtonInvisible(butRect))
            {
                SoundDefOf.TickTiny.PlayOneShotOnCamera();
                selected = true;
            }
            var color = GUI.color;
            GUI.color = Color.white;
            CheckBoxMultiDraw(rect.xMax - 24f, rect.y, state, false);
            GUI.color = color;
            var butRect2 = new Rect(rect.xMax - 24f, rect.y, 24f, 24f);
            if (Verse.Widgets.ButtonInvisible(butRect2))
            {
                var nextState = state.NextState();
                if ((nextState == MultiCheckboxState.Off) || (state == MultiCheckboxState.Partial))
                    SoundDefOf.CheckboxTurnedOn.PlayOneShotOnCamera();
                else
                    SoundDefOf.CheckboxTurnedOff.PlayOneShotOnCamera();

                state = nextState;
            }

            return selected && !flag;
        }

        public static bool MinimizeButtonFor(Rect rectToMinimize, bool hasCloseButton)
        {
            const float buttonWidthAndHeight = 18f;

            var x = rectToMinimize.x + rectToMinimize.width - buttonWidthAndHeight - 4f;
            if (hasCloseButton)
                x -= buttonWidthAndHeight + 4f;

            var butRect = new Rect(x, rectToMinimize.y + 4f, buttonWidthAndHeight, buttonWidthAndHeight);

            return Verse.Widgets.ButtonImage(butRect, Minus);
        }

        public static bool LabelSelectable(Rect rect, string label, ref bool selected,
            TextAnchor textAnchor = TextAnchor.MiddleLeft)
        {
            if (selected)
                DrawHighlightColor(rect, Color.green, 0.5f);
            else
                Verse.Widgets.DrawHighlight(rect.ContractedByButLeft(5f));

            Verse.Widgets.DrawHighlightIfMouseover(rect);

            GenUI.SetLabelAlign(textAnchor);
            Verse.Widgets.Label(rect, label);
            GenUI.ResetLabelAlign();


            var flag = selected;
            var butRect = rect;
            butRect.width -= 5f;
            if (!selected && Verse.Widgets.ButtonInvisible(butRect))
            {
                SoundDefOf.TickTiny.PlayOneShotOnCamera();
                selected = true;
            }

            return selected && !flag;
        }

        public static void DrawHighlightColor(Rect rect, Color color, float alpha)
        {
            var savedColor = GUI.color;
            var newColor = color;
            newColor.a = alpha;
            GUI.color = newColor;
            GUI.DrawTexture(rect, TexUI.HighlightTex);
            GUI.color = savedColor;
        }
    }
}