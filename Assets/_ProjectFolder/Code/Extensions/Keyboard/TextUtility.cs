using System;
using TMPro;

public static class TextUtility
{
    public static object ConvertEnum(this Enum from, Enum to)
    {
        string enumName = from.ToString();

        if (Enum.TryParse(to.GetType(), enumName, out object result)) return result;
        else return null;
    }
    public static void SetText(this TMP_InputField input, string text)
    {
        switch (input.contentType)
        {
            default: input.SetTextWithoutNotify(text); break;

            case TMP_InputField.ContentType.EmailAddress: input.SetTextWithoutNotify(text.ToLower()); break;

            case TMP_InputField.ContentType.IntegerNumber:

                if (int.TryParse(text, out int number))
                    input.SetTextWithoutNotify(number.ToString());

            break;

            case TMP_InputField.ContentType.Name:

                string[] words = text.Split(' ');

                for (int i = 0; i < words.Length; i++)
                {
                    if (!string.IsNullOrEmpty(words[i]))
                        words[i] = char.ToUpper(words[i][0]) + words[i].Substring(1).ToLower();
                }

                input.SetTextWithoutNotify(string.Join(" ", words));

            break;
        }
    }
}