﻿namespace RelativeNumber
{
    using System;
    using System.Globalization;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;
    using Microsoft.VisualStudio.Text.Editor;
    using Microsoft.VisualStudio.Text.Classification;

    /// <summary>
    /// A class detailing the margin's visual definition including both size and content.
    /// </summary>
    internal class RelativeNumber : Canvas, IWpfTextViewMargin
    {
        internal const string MarginName = "RelativeNumber";
        private IWpfTextView textView;
        private IEditorFormatMap formatMap;
        private bool isDisposed;

        private int lastCursorLine = -1;

        public RelativeNumber(IWpfTextView textView, IEditorFormatMap formatMap)
        {
            this.textView = textView;
            this.formatMap = formatMap;

            this.ClipToBounds = true;
            TextOptions.SetTextFormattingMode(this, TextFormattingMode.Ideal);
            TextOptions.SetTextRenderingMode(this, TextRenderingMode.ClearType);

            textView.Caret.PositionChanged += OnCaretPositionChanged;
            textView.LayoutChanged += OnLayoutChanged;
            textView.ViewportHeightChanged += (sender, args) => ApplyNumbers();
            formatMap.FormatMappingChanged += (sender, args) => ApplyNumbers();
        }

        private void OnCaretPositionChanged(object sender, CaretPositionChangedEventArgs e)
        {
            var currentCursorLine = CursorLineIndex;

            if (lastCursorLine != currentCursorLine)
            {
                lastCursorLine = currentCursorLine;
                ApplyNumbers();
            }
        }

        private void OnLayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
        {
            if (e.VerticalTranslation || e.NewOrReformattedLines.Count > 1)
            {
                ApplyNumbers();
            }
        }

        private int CursorLineIndex
        {
            get
            {
                return textView.TextSnapshot.GetLineNumberFromPosition(
                           textView.Caret.Position.BufferPosition.Position
                        ) + 1;
            }
        }

        private void ApplyNumbers()
        {
            // Get the visual styles
            var lineNumberColorScheme = formatMap.GetProperties("Line Number");
            var backColor = (SolidColorBrush)lineNumberColorScheme[EditorFormatDefinition.BackgroundBrushId];
            var foreColor = (SolidColorBrush)lineNumberColorScheme[EditorFormatDefinition.ForegroundBrushId];
            var fontFamily = textView.FormattedLineSource.DefaultTextProperties.Typeface.FontFamily;
            var fontSize = textView.FormattedLineSource.DefaultTextProperties.FontRenderingEmSize;

            Children.Clear();

            var currentCursorLine = CursorLineIndex;
            var viewCursorIndex = textView.TextViewLines.GetIndexOfTextLine(textView.Caret.ContainingTextViewLine);
            var viewTotalLines = textView.TextViewLines.Count;
            var viewStartLine = textView.TextViewLines.FirstVisibleLine.Start.GetContainingLine().LineNumber;
            var viewEndLine = textView.TextViewLines.LastVisibleLine.End.GetContainingLine().LineNumber;
            var totalLineCount = textView.TextSnapshot.LineCount;
            var numberCharactersLineCount = (totalLineCount == 0) ? 1 : (int)Math.Log10(totalLineCount) + 1 + 1;

            this.Width = CalculateWidth(string.Format(CultureInfo.CurrentCulture, "{0:X" + numberCharactersLineCount + "}", totalLineCount), fontFamily, fontSize);
            this.Background = backColor;

            for (var i = 0; i < viewTotalLines; i++)
            {
                var width = numberCharactersLineCount;
                int displayNumber;
                if (currentCursorLine > viewStartLine && currentCursorLine < viewEndLine)
                    displayNumber = Math.Abs(viewCursorIndex - i);
                else
                    displayNumber = Math.Abs(currentCursorLine - viewStartLine - i);

                if (displayNumber == 0)
                {
                    displayNumber = currentCursorLine;
                    width = numberCharactersLineCount * -1;
                }

                var lineNumber = ConstructLineNumber(displayNumber, width, fontFamily, fontSize, foreColor);
                var top = textView.TextViewLines[i].TextTop - textView.ViewportTop;
                SetTop(lineNumber, top);
                Children.Add(lineNumber);
            }
        }

        private static Label ConstructLineNumber(int displayNumber, int width, FontFamily fontFamily, double fontSize, Brush foreColor)
        {
            var label = new Label
            {
                FontFamily = fontFamily,
                FontSize = fontSize,
                Foreground = foreColor,
                Content = string.Format(CultureInfo.CurrentCulture, "{0," + width + "}", displayNumber),
                Padding = new Thickness(0, 1, 0, 0)
            };
            return label;
        }

        private static double CalculateWidth(string displayNumber, FontFamily fontFamily, double fontSize)
        {
            var formattedText = new FormattedText(
                displayNumber,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(fontFamily.Source),
                fontSize,
                Brushes.Black);

            return formattedText.Width;
        }
        /// <summary>
        /// The <see cref="System.Windows.FrameworkElement"/> that implements the visual representation
        /// of the margin.
        /// </summary>
        public FrameworkElement VisualElement
        {
            // Since this margin implements Canvas, this is the object which renders
            // the margin.
            get
            {
                ThrowIfDisposed();
                return this;
            }
        }

        public double MarginSize
        {
            // Since this is a horizontal margin, its width will be bound to the width of the text view.
            // Therefore, its size is its height.
            get
            {
                ThrowIfDisposed();
                return this.ActualHeight;
            }
        }

        public bool Enabled
        {
            // The margin should always be enabled
            get
            {
                ThrowIfDisposed();
                return true;
            }
        }

        /// <summary>
        /// Returns an instance of the margin if this is the margin that has been requested.
        /// </summary>
        /// <param name="marginName">The name of the margin requested</param>
        /// <returns>An instance of RelativeNumber or null</returns>
        public ITextViewMargin GetTextViewMargin(string marginName)
        {
            return (marginName == MarginName) ? this : null;
        }

        public void Dispose()
        {
            if (!isDisposed)
            {
                GC.SuppressFinalize(this);
                isDisposed = true;
            }
        }

        private void ThrowIfDisposed()
        {
            if (isDisposed)
                throw new ObjectDisposedException(MarginName);
        }
    }
}