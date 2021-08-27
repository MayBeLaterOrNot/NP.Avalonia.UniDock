﻿// (c) Nick Polyak 2021 - http://awebpros.com/
// License: MIT License (https://opensource.org/licenses/MIT)
//
// short overview of copyright rules:
// 1. you can use this framework in any commercial or non-commercial 
//    product as long as you retain this copyright message
// 2. Do not blame the author of this software if something goes wrong. 
// 
// Also, please, mention this software in any documentation for the 
// products that use it.

using Avalonia.Controls;
using NP.Avalonia.Visuals;
using NP.Utilities;
using System;
using System.Xml.Serialization;

namespace NP.Avalonia.UniDock.Serialization
{
    public class WindowParams
    {
        [XmlAttribute]
        public string? FullWindowType { get; set; }

        public Point2D? TopLeft { get; set; }

        public Point2D? Size { get; set; }

        [XmlAttribute]
        public string? Title { get; set; }

        [XmlAttribute]
        public WindowState TheWindowState { get; set; }

        [XmlAttribute]
        public string? OwnerWindowId { get; set; }

        [XmlAttribute]
        public string? WindowId { get; set; }

        [XmlAttribute]
        public string? DockChildWindowOwnerId { get; set; }

        [XmlAttribute]
        public string? TopLevelGroupId { get; set; }
    }

    public static class WindowParamsHelper
    {
        public static WindowParams ToWindowParams(this Window w)
        {
            WindowParams wp = new WindowParams();

            wp.FullWindowType = w.GetType().FullName;

            wp.TopLeft = w.Position.ToPoint2D();

            wp.Size = w.Bounds.Size.ToPoint2D();

            wp.Title = w.Title;

            wp.TheWindowState = w.WindowState;

            wp.WindowId = DockAttachedProperties.GetWindowId(w);

            if (w.Owner != null)
            {
                wp.OwnerWindowId = DockAttachedProperties.GetWindowId(w.Owner);
            }

            Window dockChildWindowOwnerWindow =
                DockAttachedProperties.GetDockChildWindowOwner(w);

            if (dockChildWindowOwnerWindow != null)
            {
                wp.DockChildWindowOwnerId =
                    DockAttachedProperties.GetWindowId(dockChildWindowOwnerWindow);
            }

            if (w is FloatingWindow dockWindow)
            {
                wp.TopLevelGroupId = dockWindow.TheDockGroup.DockId;
            }

            return wp;
        }

        public static void  SetWindowFromParams(this Window w, WindowParams wp)
        {
            Type windowType = ReflectionUtils.RestoreType(wp.FullWindowType);

            if (windowType != w.GetType())
            {
                throw new Exception($"ERROR window types do not match for the window titled {w.Title}.");
            }

            w.Position = wp.TopLeft.ToPixelPoint();
            w.Width = wp.Size!.X;
            w.Height = wp.Size.Y;

            w.Title = wp.Title;
            w.WindowState = wp.TheWindowState;

            string? windowId = wp.WindowId;

            if (windowId != null)
            {
                DockAttachedProperties.SetWindowId(w, windowId);
            }
        }

        public static Window? RestoreWindow(this WindowParams wp)
        {
            Type windowType = ReflectionUtils.RestoreType(wp.FullWindowType);

            // only floating windows can be restored
            if (!typeof(FloatingWindow).IsAssignableFrom(windowType))
            {
                return null;
            }

            FloatingWindow w = (FloatingWindow) Activator.CreateInstance(windowType)!;

            w.SetWindowFromParams(wp);

            w.TheDockGroup.DockId = wp.TopLevelGroupId!;

            return w;
        }
    }
}