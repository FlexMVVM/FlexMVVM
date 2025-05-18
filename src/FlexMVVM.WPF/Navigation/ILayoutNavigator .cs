﻿using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace FlexMVVM.Navigation
{
    public interface ILayoutNavigator
    {
        void SetRootLayout();
        void RootLayout();
        void NavigateTo(string url);
        UIElement CreateLayout(string url);
    }

    public class LayoutNavigator : ILayoutNavigator
    {
        public void SetRootLayout()
        {
            Type winType = NameContainer.RegisterType["FlexFrameworkWindow"];
            Type contentType = NameContainer.RootLayout;

            var winObject = (UIElement)NameContainer.ServiceProvider.GetService (winType);
            var layOutObject = (UIElement)NameContainer.ServiceProvider.GetService (contentType);
            if (winObject is Window window)
            {
                window.Content = layOutObject;
                RootLayout ();
            }
        }

        public void RootLayout()
        {
            Type rootLayoutType = NameContainer.RootLayout;

            if (rootLayoutType == null)
            {

                return;
            }

            var layOutObject = (UIElement)NameContainer.ServiceProvider.GetService (rootLayoutType);
          
            if (layOutObject is DockPanel dockPanel)
            {
                Contentemove (dockPanel);
                bool _isGroupedWithRegion = IsGroupedWithRegion (rootLayoutType.Namespace);
                if (_isGroupedWithRegion == false)
                    return;
                LayerPress (rootLayoutType);
            }
        }

        public void NavigateTo(string url)
        {
            string _url = url.Replace ('/', '.');

            Type rootLayoutType = NameContainer.RootLayout;

            UIElement layout;
            if(rootLayoutType == null)
            {
                layout = CreateLayout (_url);
                Type winType = NameContainer.RegisterType["FlexFrameworkWindow"];
                var winObject = (UIElement)NameContainer.ServiceProvider.GetService (winType);
                if (winObject is Window window)
                {
                    window.Content = layout;
                    RootLayout ();
                }
                return;
            }

            var layOutObject = (UIElement)NameContainer.ServiceProvider.GetService (rootLayoutType);

            layout = CreateLayout (_url);
            if (layOutObject is DockPanel dockPanel)
            {
                Contentemove (dockPanel);
                dockPanel.Children.Add (layout);
            }
        }

        public UIElement CreateLayout(string url)
        {
            try
            {
                if (NameContainer.RegisterType.Keys.Any(x=>x.Contains(url)) == false)
                    throw new Exception ("Module 등록되지 않은 url 입니다.");

                bool _isGroupedWithLayout = IsGroupedWithLayout (url);
                bool _isGroupedWithRegion = IsGroupedWithRegion (url);

                if((_isGroupedWithLayout || _isGroupedWithRegion) == false)
                    throw new Exception ("등록 된 Layout 또는 Page가 없습니다.");

                string typeNameSpace = _isGroupedWithLayout ? GetLayoutString (url) : GetRegionString(url);
                Type contentType = NameContainer.RegisterType[typeNameSpace];
                if (_isGroupedWithLayout && _isGroupedWithRegion)
                {
                    LayerPress (contentType);
                }

                string moduleName = contentType.Assembly.GetName ().Name;
                int layoutCnt = (contentType.Namespace.Split ('.').Length - moduleName.Split ('.').Length);

                UIElement rootPanel = null;
                for (int i = 0; i < layoutCnt; i++)
                {
                    var parentFolderUrl = RemoveLastSegment (contentType.Namespace);
                    rootPanel = CreateLayout ($"{parentFolderUrl}");
                }
                UIElement layOutObject = (UIElement)NameContainer.ServiceProvider.GetService (contentType);

                if (rootPanel != null)
                {
                    Contentemove (rootPanel);
                    ((DockPanel)rootPanel).Children.Add (layOutObject);

                    return rootPanel;
                }

                return layOutObject;
            }
            catch (Exception ex)
            {
            }
            return null;
        }

        private string GetLayoutString(string url)
        {
            if (url.Split ('.').Last () == "Layout")
                return url;

            return $"{url}.Layout";
        }

        private string GetRegionString(string url)
        {
            if (url.Split ('.').Last () == "Region")
                return url;

            return $"{url}.Region";
        }

        private bool IsGroupedWithLayout(string url)
        {
            if(url.Split('.').Last() == "Layout")
                return true;
            return NameContainer.RegisterType.ContainsKey (GetLayoutString (url));
        }
        private bool IsGroupedWithRegion(string url)
        {
            if (url.Split ('.').Last () == "Region")
                return true;
            return NameContainer.RegisterType.ContainsKey (GetRegionString (url));
        }

        private void LayerPress(Type layoutType)
        {
            var _region = layoutType.Assembly.DefinedTypes.Where (x => x.Namespace == layoutType.Namespace)
                                                         .First (x => x.Name == "Region");

            var layOutObject = (UIElement)NameContainer.ServiceProvider.GetService (layoutType);
            var regionObject = (UIElement)NameContainer.ServiceProvider.GetService (_region);
            if (layOutObject is DockPanel dockPanel)
            {
                Contentemove (dockPanel);
                dockPanel.Children.Add(regionObject);
            }
        }

        private void Contentemove(UIElement dockPanel) {
            DockPanel _dockPanel = (DockPanel)dockPanel;
            UIElement dockofRegion = null;
            foreach (UIElement child in _dockPanel.Children)
            {
                // DockPanel.Dock 의 설정 상태를 확인
                var valueSource = DependencyPropertyHelper.GetValueSource (child, DockPanel.DockProperty);

                // Local 값으로 설정되지 않은 경우
                if (!valueSource.BaseValueSource.HasFlag (BaseValueSource.Local))
                {
                    dockofRegion = child;
                }
            }
            if (dockofRegion != null)
            {
                _dockPanel.Children.Remove (dockofRegion);
            }
        }

        private string RemoveLastSegment(string path)
        {
            int lastDot = path.LastIndexOf ('.');
            return lastDot >= 0 ? path.Substring (0, lastDot) : path;
        }
    }
}
