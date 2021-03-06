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

using System;
using Windows.UI;
using Windows.UI.StartScreen;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using SoundByte.Core.Helpers;

namespace SoundByte.Core.Dialogs
{
    public sealed partial class PinTileDialog
    {
        public PinTileDialog(string tileId, string title, string param, Uri imageUri)
        {
            TileId = tileId;
            TileTitle = title;
            Param = param;
            ImageUri = imageUri;
            ImageSource = new BitmapImage(ImageUri);

            InitializeComponent();
        }

        public string TileId { get; set; }

        public string TileTitle { get; set; }

        public string Param { get; set; }

        public Uri ImageUri { get; set; }

        public BitmapImage ImageSource { get; set; }

        public void ToggleColor(object sender, RoutedEventArgs e)
        {
            PreviewText.Foreground = ((ToggleSwitch) sender).IsOn
                ? new SolidColorBrush(Colors.Black)
                : new SolidColorBrush(Colors.White);
        }

        public async void PinTile()
        {
            var tileId = TileId;
            var tileTitle = TileTextBox.Text;
            var param = Param;
            var imageUri = await ImageHelper.CreateCachedImageAsync(ImageUri.AbsoluteUri, "SoundCloudImg_" + tileId);
            var tileForground = ColorToggleSwitch.IsOn ? ForegroundText.Dark : ForegroundText.Light;

            // Check that the image is not false
            if (imageUri == null)
                return;

            // Create a secondary tile
            var liveTile = new SecondaryTile(tileId, tileTitle, param, imageUri, TileSize.Default)
            {
                VisualElements =
                {
                    ForegroundText = tileForground,
                    ShowNameOnSquare310x310Logo = true,
                    ShowNameOnWide310x150Logo = true,
                    ShowNameOnSquare150x150Logo = true
                }
            };

            Hide();

            await liveTile.RequestCreateAsync();
        }
    }
}