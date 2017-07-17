using PrepareLanding.Extensions;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace PrepareLanding.Gui
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
                        break;

                    case MultiCheckboxState.Partial:
                        state = MultiCheckboxState.Off;
                        break;

                    case MultiCheckboxState.Off:
                        state = MultiCheckboxState.On;
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
                if (nextState == MultiCheckboxState.Off || state == MultiCheckboxState.Partial)
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

        public static bool LabelSelectable(Rect rect, string label, ref bool selected, TextAnchor textAnchor = TextAnchor.MiddleLeft)
        {
            if (selected)
            {
                DrawHighlightColor(rect, Color.green, 0.5f);
            }
            else
            {
                Verse.Widgets.DrawHighlight(rect.ContractedByButLeft(5f));
            }

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
    

        #region TEXTFIELDNUMERIC

        public static void TextFieldNumericLabeled<T>(Rect rect, string label, ref T val, ref string buffer,
            float min = 0f, float max = 1E+09f, bool useAnchor = true) where T : struct
        {
            var rect2 = rect.LeftHalf().Rounded();
            var rect3 = rect.RightHalf().Rounded();

            var anchor = Text.Anchor;
            if (useAnchor)
                Text.Anchor = TextAnchor.MiddleRight;

            Verse.Widgets.Label(rect2, label);

            if (useAnchor)
                Text.Anchor = anchor;

            TextFieldNumeric(rect3, ref val, ref buffer, min, max);
        }

        public static void TextFieldNumeric<T>(Rect rect, ref T val, ref string buffer, float min = 0f,
            float max = 1E+09f) where T : struct
        {
            if (buffer == null)
                buffer = val.ToString();
            var text = "TextField" + rect.y.ToString("F0") + rect.x.ToString("F0");
            GUI.SetNextControlName(text);
            var text2 = GUI.TextField(rect, buffer, Text.CurTextFieldStyle);
            if (GUI.GetNameOfFocusedControl() != text)
            {
                ResolveParseNow(buffer, ref val, ref buffer, min, max, true);
            }
            else if (text2 != buffer && IsPartiallyOrFullyTypedNumber(ref val, text2, min, max))
            {
                buffer = text2;
                if (text2.IsFullyTypedNumber<T>())
                    ResolveParseNow(text2, ref val, ref buffer, min, max, false);
            }
        }

        private static bool IsPartiallyOrFullyTypedNumber<T>(ref T val, string s, float min, float max)
        {
            if (s == string.Empty)
                return true;
            if (s[0] == '-' && min >= 0f)
                return false;
            if (s.Length > 1 && s[s.Length - 1] == '-')
                return false;
            if (s == "00")
                return false;
            if (s.Length > 12)
                return false;
            if (typeof(T) == typeof(float))
            {
                var num = s.CharacterCount('.');
                if (num <= 1 && s.ContainsOnlyCharacters("-.0123456789"))
                    return true;
            }
            return s.IsFullyTypedNumber<T>();
        }

        private static void ResolveParseNow<T>(string edited, ref T val, ref string buffer, float min, float max,
            bool force)
        {
            if (typeof(T) == typeof(int))
            {
                if (edited.NullOrEmpty())
                {
                    ResetValue(edited, ref val, ref buffer, min, max);
                    return;
                }
                int num;
                if (int.TryParse(edited, out num))
                {
                    val = (T) (object) Mathf.RoundToInt(Mathf.Clamp(num, min, max));
                    buffer = ToStringTypedIn(val);
                    return;
                }
                if (force)
                    ResetValue(edited, ref val, ref buffer, min, max);
            }
            else if (typeof(T) == typeof(float))
            {
                float value;
                if (float.TryParse(edited, out value))
                {
                    val = (T) (object) Mathf.Clamp(value, min, max);
                    buffer = ToStringTypedIn(val);
                    return;
                }
                if (force)
                    ResetValue(edited, ref val, ref buffer, min, max);
            }
            else
            {
                Log.Error("TextField<T> does not support " + typeof(T));
            }
        }

        private static void ResetValue<T>(string edited, ref T val, ref string buffer, float min, float max)
        {
            val = default(T);
            if (min > 0f)
                val = (T) (object) Mathf.RoundToInt(min);
            if (max < 0f)
                val = (T) (object) Mathf.RoundToInt(max);
            buffer = ToStringTypedIn(val);
        }

        private static string ToStringTypedIn<T>(T val)
        {
            if (typeof(T) == typeof(float))
                return ((float) (object) val).ToString("0.##########");
            return val.ToString();
        }

        #endregion TEXTFIELDNUMERIC
    }
}