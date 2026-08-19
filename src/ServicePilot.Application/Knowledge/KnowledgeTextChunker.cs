using System.Text;

namespace ServicePilot.Application.Knowledge;

public sealed class KnowledgeTextChunker
{
    public const int TargetCharacters = 1200;
    public const int MaximumCharacters = 1800;
    public const int OverlapCharacters = 200;

    public IReadOnlyList<KnowledgeTextChunk> Chunk(
        IReadOnlyList<ExtractedPdfPage> pages)
    {
        List<KnowledgeTextChunk> chunks = [];

        foreach (ExtractedPdfPage page in pages)
        {
            string text = NormalizeWhitespace(page.Text);
            int start = 0;

            while (start < text.Length)
            {
                int end = FindEnd(text, start);
                string content = text[start..end].Trim();
                if (content.Length > 0)
                {
                    chunks.Add(new KnowledgeTextChunk(
                        page.PageNumber,
                        content));
                }

                if (end >= text.Length)
                {
                    break;
                }

                int nextStart = Math.Max(
                    start + 1,
                    end - OverlapCharacters);
                while (nextStart < end
                    && !char.IsWhiteSpace(text[nextStart]))
                {
                    nextStart++;
                }

                start = nextStart < end
                    ? nextStart + 1
                    : end;
            }
        }

        return chunks;
    }

    private static int FindEnd(string text, int start)
    {
        int remaining = text.Length - start;
        if (remaining <= MaximumCharacters)
        {
            return text.Length;
        }

        int target = start + TargetCharacters;
        int maximum = Math.Min(
            text.Length,
            start + MaximumCharacters);

        for (int index = target; index < maximum; index++)
        {
            if (text[index] is '.' or '!' or '?'
                && (index + 1 == text.Length
                    || char.IsWhiteSpace(text[index + 1])))
            {
                return index + 1;
            }
        }

        for (int index = maximum; index > target; index--)
        {
            if (char.IsWhiteSpace(text[index - 1]))
            {
                return index - 1;
            }
        }

        return maximum;
    }

    private static string NormalizeWhitespace(string value)
    {
        StringBuilder result = new(value.Length);
        bool previousWasWhitespace = true;

        foreach (char character in value)
        {
            if (char.IsWhiteSpace(character))
            {
                if (!previousWasWhitespace)
                {
                    result.Append(' ');
                }

                previousWasWhitespace = true;
            }
            else
            {
                result.Append(character);
                previousWasWhitespace = false;
            }
        }

        return result.ToString().Trim();
    }
}

public sealed record KnowledgeTextChunk(
    int PageNumber,
    string Content);