﻿using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace TeleCore
{
    internal class EffectBuilderWindowContainer : UIElement
    {
        private Window parentWindow;

        private readonly UITopBar topBar;
        private readonly EffectCanvas canvas;

        private readonly FleckMoteBrowser browser;

        public EffectBuilderWindowContainer(Window parent, Rect rect, UIElementMode mode) : base(rect, mode)
        {
            //
            //bgColor = TColor.WindowBGFillColor;
            //borderColor = TColor.WindowBGBorderColor;

            this.parentWindow = parent;
            this.bgColor = TColor.BGDarker;
            this.borderColor = Color.clear;
            //
            canvas = new EffectCanvas(UIElementMode.Static);
            browser = new FleckMoteBrowser(UIElementMode.Static);

            //
            var buttonMenus = new List<TopBarButtonMenu>();
            //File
            var fileOptions = new List<TopBarButtonOption>();
            fileOptions.Add(new TopBarButtonOption("New", () =>
            {

            }));
            fileOptions.Add(new TopBarButtonOption("Save/Load", () =>
            {

            }));

            buttonMenus.Add(new TopBarButtonMenu("File", fileOptions));

            //View
            var viewOptions = new List<TopBarButtonOption>();
            buttonMenus.Add(new TopBarButtonMenu("View", viewOptions));

            topBar = new UITopBar(buttonMenus);
            topBar.AddCloseButton(() =>
            {
                parentWindow.Close();
            });
        }

        public void Notify_Reopened()
        {

        }

        protected override void DrawTopBarExtras(Rect topRect)
        {

        }

        protected override void DrawContentsBeforeRelations(Rect inRect)
        {
            Widgets.BeginGroup(inRect);
            {
                Rect canvasRect = new Rect(0, 0, 900, 900);
                Rect objectBrowserRect = new Rect(canvasRect.xMax-1, canvasRect.y, 300, canvasRect.height + 1);

                //
                canvas.DrawElement(canvasRect);
                browser.DrawElement(objectBrowserRect);
            }
            Widgets.EndGroup();

            //
            topBar.DrawElement(TopRect);
        }
    }
}
