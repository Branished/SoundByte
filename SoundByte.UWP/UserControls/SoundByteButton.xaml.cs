﻿/* |----------------------------------------------------------------|
 * | Copyright (c) 2017, Grid Entertainment                         |
 * | All Rights Reserved                                            |
 * |                                                                |
 * | This source code is to only be used for educational            |
 * | purposes. Distribution of SoundByte source code in             |
 * | any form outside this repository is forbidden. If you          |
 * | would like to contribute to the SoundByte source code, you     |
 * | are welcome.                                                   |
 * |----------------------------------------------------------------|
 */

using Windows.UI.Xaml;

namespace SoundByte.UWP.UserControls
{
    public sealed partial class SoundByteButton
    {
        public delegate void ClickEventHandler(object sender, RoutedEventArgs e);

        public static readonly DependencyProperty LabelProperty = DependencyProperty.Register("Label", typeof(string), typeof(SoundByteButton), null);
        public static readonly DependencyProperty GlyphProperty = DependencyProperty.Register("Glyph", typeof(string), typeof(SoundByteButton), null);

        /// <summary>
        /// The label to show on the button
        /// </summary>
        public string Label
        {
            get => GetValue(LabelProperty) as string;
            set => SetValue(LabelProperty, value);
        }

        /// <summary>
        /// Handles the button click event
        /// </summary>
        public event ClickEventHandler Click;

        /// <summary>
        /// The glyph to show on the button
        /// </summary>
        public string Glyph
        {
            get => this.GetValue(GlyphProperty) as string;
            set => this.SetValue(GlyphProperty, value);
        }

        public SoundByteButton()
        {
            this.InitializeComponent();

            MainButton.Click += (sender, args) =>
            {
                Click?.Invoke(sender, args);
            };
        }
    }
}
