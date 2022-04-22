// Copyright (c) Microsoft Corporation. All rights reserved.
//
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.Graphics.Canvas.UI.Composition;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.UI;
using Windows.UI.Composition;
using Windows.UI.Core;
using Windows.UI.Popups;


namespace CompositionExample
{
    class App : IFrameworkView
    {
        bool RenderDone = false;
        CoreWindow window;
        Compositor compositor;
        ContainerVisual rootVisual;
        CompositionTarget compositionTarget;
        CanvasDevice device;
        CompositionGraphicsDevice compositionGraphicsDevice;
        RelationshipHandler relationshipHandler;
        //List<RelationshipLine> relationshipLines;
        bool pressOccured = false;
        Windows.UI.Input.PointerPoint MouseOrigin;


        private Vector2 PointerDragOffset;

        CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        Random rnd = new Random();
        Vector2 OgSize;
        int ScrollOffset = 0;
        Size size;
        public void Initialize(CoreApplicationView applicationView)
        {

            
            //relationshipLines = new List<RelationshipLine>();
            applicationView.Activated += applicationView_Activated;
            RenderDone = false;
            
            
        }

        private void Window_PointerWheelChanged(CoreWindow sender, PointerEventArgs args)
        {
            RenderDone = false;
            ScrollOffset += args.CurrentPoint.Properties.MouseWheelDelta;
            var ignoredTask = UpdateVisualsLoop();
            
        }

        private void Window_SizeChanged(CoreWindow sender, WindowSizeChangedEventArgs args)
        {
            RenderDone = false;
            var ignoredTask = UpdateVisualsLoop();
        }

        public void Uninitialize()
        {
            //swapChainRenderer?.Dispose();
            cancellationTokenSource.Cancel();
        }

        void applicationView_Activated(CoreApplicationView sender, IActivatedEventArgs args)
        {
            CoreWindow.GetForCurrentThread().Activate();
        }

        public void Load(string entryPoint)
        {
        }

        public void Run()
        {
            CoreWindow.GetForCurrentThread().Dispatcher.ProcessEvents(CoreProcessEventsOption.ProcessUntilQuit);

        }

        public async void SetWindow(CoreWindow window)
        {
            this.window = window;
            OgSize = new Vector2((float)window.Bounds.Width, (float)window.Bounds.Height);
            Clipboard.ContentChanged += Clipboard_ContentChanged;
            window.SizeChanged += Window_SizeChanged;
            window.PointerWheelChanged += Window_PointerWheelChanged;

            if (!Windows.Foundation.Metadata.ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 2))
            {
                var dialog = new MessageDialog("This version of Windows does not support the Composition APIs.");
                await dialog.ShowAsync();
                CoreApplication.Exit();
                return;
            }

            window.PointerPressed += Window_PointerPressed;

            CoreApplication.Suspending += CoreApplication_Suspending;
            DisplayInformation.DisplayContentsInvalidated += DisplayInformation_DisplayContentsInvalidated;

            compositor = new Compositor();

            CreateDevice();

            size = new Size(window.Bounds.Width,window.Bounds.Height);
            size.Width *= 0.10;
            size.Height *= 0.10;
            
            rootVisual = compositor.CreateContainerVisual();
            compositionTarget = compositor.CreateTargetForCurrentView();
            compositionTarget.Root = rootVisual;
            relationshipHandler = new RelationshipHandler(compositor, compositionGraphicsDevice, size);
            RenderDone = false;
            var ignoredTask = UpdateVisualsLoop();
        }

        private void Clipboard_ContentChanged(object sender, object e)
        {
            size = new Size(window.Bounds.Width, window.Bounds.Height);
            size.Width *= 0.10;
            size.Height *= 0.10;

            rootVisual = compositor.CreateContainerVisual();
            
            compositionTarget.Root = rootVisual;
            relationshipHandler=  new RelationshipHandler(compositor, compositionGraphicsDevice, size);
            
            rootVisual.Children.InsertAtTop(relationshipHandler.Visual);
            RenderDone = false;
            //relationshipHandler.Clear();
            
            var ignoredTask = UpdateVisualsLoop();

        }
      

        


        async Task UpdateVisualsLoop()
        {
            var token = cancellationTokenSource.Token;

            while (!token.IsCancellationRequested && !RenderDone)
            {
                
                    UpdateVisual(relationshipHandler.Visual, relationshipHandler.Size);
                    relationshipHandler.Visual.Opacity = 1;
                


                await Task.Delay(TimeSpan.FromSeconds(2));
                RenderDone = true;
            }
        }

        void UpdateVisual(Visual visual, Size size)
        {
            UpdateVisualPosition(visual, size);
            UpdateVisualOpacity(visual);
        }

        void UpdateVisualPosition(Visual visual, Size size)
        {
            var oldOffset = visual.Offset;
            Vector2 newSize = new Vector2((float)window.Bounds.Width, (float)window.Bounds.Height);
            Vector2 oldVisSize = new Vector2((float)size.Width, (float)size.Height);
            Vector2 newVisSize =  (oldVisSize * (newSize / OgSize));
            newVisSize.X += ScrollOffset;
            newVisSize.Y += ScrollOffset;
            var newOffset = new Vector3(
                (float)(((visual.CenterPoint.X) * newVisSize.X)),
                (float)(((visual.CenterPoint.Y) * newVisSize.Y)),
                0);
            visual.Offset = newOffset;


            if (newVisSize != oldVisSize)
            {
                AnimateSizeChange(visual, oldVisSize, newVisSize);
                AnimateOffset(visual, oldOffset, newOffset);
            }
                
        }

        
        void UpdateVisualOpacity(Visual visual)
        {
            var oldOpacity = visual.Opacity;
            var newOpacity = 1;

            var animation = compositor.CreateScalarKeyFrameAnimation();
            animation.InsertKeyFrame(0, oldOpacity);
            animation.InsertKeyFrame(1, newOpacity);

            visual.Opacity = newOpacity;
            
            visual.StartAnimation("Opacity", animation);
        }
        void AnimateSizeChange(Visual visual, Vector2 oldSize, Vector2 newSize)
        {
            var animation = compositor.CreateVector2KeyFrameAnimation();
            animation.InsertKeyFrame(1, oldSize);
            animation.InsertKeyFrame(1, newSize);
            animation.Duration = TimeSpan.FromSeconds(0.5);

            visual.StartAnimation("Size", animation);
        }

        void AnimateOffset(Visual visual, Vector3 oldOffset, Vector3 newOffset)
        {
            var animation = compositor.CreateVector3KeyFrameAnimation();
            animation.InsertKeyFrame(0, oldOffset);
            animation.InsertKeyFrame(1, newOffset);
            animation.Duration = TimeSpan.FromSeconds(1);

            visual.StartAnimation("Offset", animation);
        }
        
        void Window_PointerPressed(CoreWindow sender, PointerEventArgs args)
        {
            if(!pressOccured)
            {
                pressOccured = true;
                MouseOrigin = args.CurrentPoint;
                PointerDragOffset = new Vector2();
                //sender.wind
            } else
            {
                PointerDragOffset.X = (float)MouseOrigin.RawPosition.X;
                PointerDragOffset.Y = (float)MouseOrigin.RawPosition.Y;
                RenderDone = false;
            }


            UpdateVisual(rootVisual, size);
                


        }


        void CoreApplication_Suspending(object sender, SuspendingEventArgs args)
        {
            try
            {
                device.Trim();
            }
            catch (Exception e) when (device.IsDeviceLost(e.HResult))
            {
                device.RaiseDeviceLost();
            }
        }

        void CreateDevice()
        {
            device = CanvasDevice.GetSharedDevice();
            device.DeviceLost += Device_DeviceLost;

            if (compositionGraphicsDevice == null)
            {
                compositionGraphicsDevice = CanvasComposition.CreateCompositionGraphicsDevice(compositor, device);
            }
            else
            {
                CanvasComposition.SetCanvasDevice(compositionGraphicsDevice, device);
            }

            //if (swapChainRenderer != null)
                //swapChainRenderer.SetDevice(device, new Size(window.Bounds.Width, window.Bounds.Height));
        }

        void Device_DeviceLost(CanvasDevice sender, object args)
        {
            device.DeviceLost -= Device_DeviceLost;
            var unwaitedTask = window.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => CreateDevice());
        }

        void DisplayInformation_DisplayContentsInvalidated(DisplayInformation sender, object args)
        {
            // The display contents could be invalidated due to a lost device, or for some other reason.
            // We check this by calling GetSharedDevice, which will make sure the device is still valid before returning it.
            // If the shared device has been lost, GetSharedDevice will automatically raise its DeviceLost event.
            CanvasDevice.GetSharedDevice();
        }
    }

    class ViewSource : IFrameworkViewSource
    {
        public IFrameworkView CreateView()
        {
            return new App();
        }

        static void Main(string[] args)
        {
            CoreApplication.Run(new ViewSource());
        }
    }
}
