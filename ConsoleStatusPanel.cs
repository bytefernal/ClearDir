using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;

namespace ClearDir
{
    public enum TextAlignment
    {
        Left,
        Center,
        Right
    }

    /// <summary>
    /// Displays panel elements (cells) arranged into rows.
    /// Cells are formatted with fixed widths and a specified text alignment.
    /// Rows are rendered as plain text lines (without extra borders) and are updated
    /// in place until the panel is finalized.
    /// 
    /// Once asynchronous operations are completed, Detach() finalizes the panel
    /// and moves the cursor to the first unused line.
    /// </summary>
    public class ConsoleStatusPanel
    {
        private readonly int baseX = 0;
        private  int baseY;
        private readonly object _lock = new object();
        private bool isFinalized = false;
        private readonly Dictionary<PanelLabels, PanelElement> _elements = new Dictionary<PanelLabels, PanelElement>();

        /// <summary>
        /// Adds a new panel element (cell). The parameters relativeX and relativeY
        /// are used to order the cells within a given row. The width sets a fixed size
        /// for the cell, and alignment determines padding.
        /// </summary>
        public void Add(PanelLabels label, string text, int relativeX, int relativeY, int width, TextAlignment alignment = TextAlignment.Left)
        {
            lock (_lock)
            {
                var element = new PanelElement(label, text, relativeX, relativeY, width, alignment);
                _elements[label] = element;
            }
        }

        /// <summary>
        /// Updates the text of an existing panel element and re-renders the panel.
        /// If the panel has been finalized, further updates are skipped.
        /// </summary>
        public void Update(PanelLabels label, string newText)
        {
            lock (_lock)
            {
                if (isFinalized)
                    return;
                if (_elements.ContainsKey(label))
                {
                    _elements[label].Text = newText;
                    Render();
                }
            }
        }

        /// <summary>
        /// Reserves space and renders the panel at the specified base coordinates.
        /// Call this after adding your elements.
        /// </summary>
        public void Initialize()
        {
            lock (_lock)
            {
                if (Console.CursorLeft != 0)
                    Console.WriteLine();

                var lines = BuildPlainLines();
                foreach (var line in lines)
                {
                    Console.WriteLine(line);
                }
                baseY = Console.CursorTop - lines.Count;
            }
        }

        /// <summary>
        /// Finalizes the panel—preventing further updates—and moves the cursor to the line immediately below it.
        /// </summary>
        public void Detach()
        {
            lock (_lock)
            {
                if (isFinalized) return;

                isFinalized = true;
                var lines = BuildPlainLines();
                int linesCount = lines.Count;
                Console.SetCursorPosition(0, baseY + linesCount);
            }
        }

        /// <summary>
        /// Groups cells by row (using relativeY) and builds plain text lines.
        /// Cells within each row (group) are sorted by relativeX and then formatted and joined with a space.
        /// </summary>
        private List<string> BuildPlainLines()
        {
            List<string> lines = new List<string>();
            // Group cells by their row order.
            var rows = _elements.Values.GroupBy(e => e.RelativeY).OrderBy(g => g.Key);
            foreach (var rowGroup in rows)
            {
                // Order the row's cells by their relative x-position.
                var rowCells = rowGroup.OrderBy(e => e.RelativeX);
                // Format each cell and join them with a space.
                string line = string.Join(" ", rowCells.Select(cell => FormatCell(cell.Text, cell.Width, cell.Alignment)));
                lines.Add(line);
            }
            return lines;
        }

        /// <summary>
        /// Renders the panel at the base coordinates.
        /// Each row is printed as a text line with the formatted cell values.
        /// </summary>
        private void Render()
        {
            Console.SetCursorPosition(baseX, baseY);
            var lines = BuildPlainLines();
            foreach (var line in lines)
            {
                Console.WriteLine(line);
            }
        }

        /// <summary>
        /// Formats text into a fixed width using the specified alignment.
        /// If text exceeds the width, it is truncated.
        /// </summary>
        private string FormatCell(string text, int width, TextAlignment alignment)
        {
            if (text.Length > width)
                text = text.Substring(0, width);
            int padding = width - text.Length;
            switch (alignment)
            {
                case TextAlignment.Left:
                    return text + new string(' ', padding);
                case TextAlignment.Right:
                    return new string(' ', padding) + text;
                case TextAlignment.Center:
                    int padLeft = padding / 2;
                    int padRight = padding - padLeft;
                    return new string(' ', padLeft) + text + new string(' ', padRight);
                default:
                    return text;
            }
        }

        /// <summary>
        /// Represents a panel element (cell) with its text, position ordering, fixed width, and alignment.
        /// </summary>
        private class PanelElement
        {
            public PanelLabels Label { get; }
            public string Text { get; set; }
            public int RelativeX { get; }
            public int RelativeY { get; }
            public int Width { get; }
            public TextAlignment Alignment { get; }

            public PanelElement(PanelLabels label, string text, int relativeX, int relativeY, int width, TextAlignment alignment)
            {
                Label = label;
                Text = text;
                RelativeX = relativeX;
                RelativeY = relativeY;
                Width = width;
                Alignment = alignment;
            }
        }
    }
}
