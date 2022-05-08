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
        //bool RenderDone = false;
        CoreWindow window;
        Compositor compositor;
        ContainerVisual rootVisual;
        CompositionTarget compositionTarget;
        CanvasDevice device;
        CompositionGraphicsDevice compositionGraphicsDevice;
        //CanvasSwapChain CanvasSwapChain;

        //RelationshipHandler relationshipHandler;
        SwapChainRenderer swapChainRenderer;

        //Pointer variables to support dragging
        bool pressOccured = false;
        //Windows.UI.Input.PointerPoint MouseOrigin;
        private Vector3 PointerDragOffset;

        CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        Random rnd = new Random();
        Vector2 OgSize;
        int ScrollOffset = 0;
        Size size;
        public void Initialize(CoreApplicationView applicationView)
        {
            applicationView.Activated += applicationView_Activated;
            //RenderDone = false;
            
        }

        private void Window_PointerWheelChanged(CoreWindow sender, PointerEventArgs args)
        {
            //swapChainRenderer.renderUpdate = true;
            ScrollOffset+= args.CurrentPoint.Properties.MouseWheelDelta;
            
            //swapChainRenderer.renderUpdate = true;
            //newVisSize = (oldVisSize * (newSize / OgSize));

            
            //var ignoredTask = UpdateVisualsLoop();

        }

        private void Window_SizeChanged(CoreWindow sender, WindowSizeChangedEventArgs args)
        {
            //swapChainRenderer.renderUpdate = true;
            //var ignoredTask = UpdateVisualsLoop();
        }

        public void Uninitialize()
        {
            swapChainRenderer?.Dispose();
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

            //Event subscribing for user interactions updating visuals
            Clipboard.ContentChanged += Clipboard_ContentChanged;
            window.SizeChanged += Window_SizeChanged;
            window.PointerWheelChanged += Window_PointerWheelChanged;
            window.PointerMoved += Window_PointerMoved;
            window.PointerReleased += Window_PointerReleased;

            

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
            swapChainRenderer = new SwapChainRenderer(compositor, size);
            swapChainRenderer.SetDevice(device, new Size(window.Bounds.Width, window.Bounds.Height));
            rootVisual = compositor.CreateContainerVisual();
            compositionTarget = compositor.CreateTargetForCurrentView();
            compositionTarget.Root = rootVisual;

        }

        

        private void Clipboard_ContentChanged(object sender, object e)
        {
            size = new Size(window.Bounds.Width, window.Bounds.Height);
            size.Width *= 0.10;
            size.Height *= 0.10;

            rootVisual = compositor.CreateContainerVisual();
            
            compositionTarget.Root = rootVisual;
            //relationshipHandler=  new RelationshipHandler(compositor, compositionGraphicsDevice, size);
            swapChainRenderer = new SwapChainRenderer(compositor,size);
            swapChainRenderer.SetDevice(device, new Size(window.Bounds.Height, window.Bounds.Width));
            //rootVisual.Children.InsertAtTop(relationshipHandler.Visual);
            rootVisual.Children.InsertAtTop(swapChainRenderer.Visual);
            rootVisual.Children.InsertAtTop(swapChainRenderer.SelectedVisual);
            //RenderDone = false;
            
            var ignoredTask = UpdateVisualsLoop();
        }
      
        async Task UpdateVisualsLoop()
        {
            var token = cancellationTokenSource.Token;

            while (!token.IsCancellationRequested)
            {
                
                if(updateSwapChainEntity)
                {
                    swapChainRenderer.SelectedVisual.Opacity = 1;
                    swapChainRenderer.Visual.Opacity = .75f;
                    UpdateVisual(swapChainRenderer.SelectedVisual, swapChainRenderer.Size);
                    
                }
                else if (!swapChainRenderer.renderUpdate)
                {
                    swapChainRenderer.Visual.Opacity = 1;
                    swapChainRenderer.SelectedVisual.Opacity = 0;
                    UpdateVisual(swapChainRenderer.Visual, swapChainRenderer.Size);

                }


                await Task.Delay(TimeSpan.FromSeconds(.05));
                //RenderDone = true;
            }
        }

        void UpdateVisual(Visual visual, Size size)
        {
            UpdateVisualPosition(visual, size);
            UpdateVisualOpacity(visual);
        }
        Vector2 newVisSize;
       //Vector2 newSize;
        Vector2 oldVisSize;
        Vector2 Proportion;
        void UpdateVisualPosition(Visual visual, Size size)
        {

            //newSize = new Vector2((float)window.Bounds.Width, (float)window.Bounds.Height);
            //oldVisSize = new Vector2((float)size.Width, (float)size.Height);
            if(newVisSize.X<=0 || newVisSize.Y <= 0)
            {
                newVisSize.X = rootVisual.Size.X + ScrollOffset;
                newVisSize.Y = rootVisual.Size.Y + ScrollOffset;
            } else
            {
                newVisSize.X += ScrollOffset;
                newVisSize.Y += ScrollOffset;
                ScrollOffset = 0;
            }
            
            

            if (oldVisSize!=newVisSize)
            {
                
                Proportion = newVisSize / size.ToVector2();
                AnimateSizeChange(visual, oldVisSize, newVisSize);
                oldVisSize = newVisSize;
            } else if (updateSwapChainEntity) {
                AnimateSizeChange(visual, oldVisSize, newVisSize);
            }
            
            
        }

        
        void UpdateVisualOpacity(Visual visual)
        {
            var oldOpacity = visual.Opacity;
            var newOpacity = 1;

            var animation = compositor.CreateScalarKeyFrameAnimation();
            animation.InsertKeyFrame(1, oldOpacity);
            animation.InsertKeyFrame(1, newOpacity);

            visual.Opacity = newOpacity;
            
            visual.StartAnimation("Opacity", animation);
        }
        void AnimateSizeChange(Visual visual, Vector2 oldSize, Vector2 newSize)
        {
            var animation = compositor.CreateVector2KeyFrameAnimation();
            animation.InsertKeyFrame(1, oldSize);
            animation.InsertKeyFrame(1, newSize);
            animation.Duration = TimeSpan.FromSeconds(0.04);

            visual.StartAnimation("Size", animation);
        }

        void AnimateOffset(Visual visual, Vector3 oldOffset, Vector3 newOffset)
        {
            var animation = compositor.CreateVector3KeyFrameAnimation();
            animation.InsertKeyFrame(1, oldOffset);
            animation.InsertKeyFrame(1, newOffset);
            animation.Duration = TimeSpan.FromSeconds(.05);

            visual.StartAnimation("Offset", animation);
        }
        private bool updateSwapChainEntity = false;
        void Window_PointerPressed(CoreWindow sender, PointerEventArgs args)
        {
            pressOccured = true;
            PointerDragOffset = new Vector3();
            
            updateSwapChainEntity = swapChainRenderer.CheckEntitySelected(args.CurrentPoint,Proportion );


        }
        private void Window_PointerReleased(CoreWindow sender, PointerEventArgs args)
        {
            pressOccured = false;
            updateSwapChainEntity = false;
            LastPoint.X = 0;
            LastPoint.Y = 0;
            
            swapChainRenderer.PointChange = new Point();
            swapChainRenderer.ResetSelection();
            //var ignoredTask = UpdateVisualsLoop();
        }
        private Vector2 LastPoint;
        private void Window_PointerMoved(CoreWindow sender, PointerEventArgs args)
        {
            if (pressOccured && LastPoint != null && !updateSwapChainEntity)
            {
                PointerDragOffset.X = (float)(rootVisual.Offset.X + (args.CurrentPoint.Position.X - LastPoint.X));
                PointerDragOffset.Y = (float)(rootVisual.Offset.Y + (args.CurrentPoint.Position.Y - LastPoint.Y));

                rootVisual.Offset = PointerDragOffset;
                LastPoint.X = (float)args.CurrentPoint.Position.X;
                LastPoint.Y = (float)args.CurrentPoint.Position.Y;
                //

            }
            else if(LastPoint !=null && pressOccured && updateSwapChainEntity)
            {
                PointerDragOffset.X = (float)(rootVisual.Offset.X + (args.CurrentPoint.Position.X - LastPoint.X));
                PointerDragOffset.Y = (float)(rootVisual.Offset.Y + (args.CurrentPoint.Position.Y - LastPoint.Y));
                swapChainRenderer.PointChange.X = (args.CurrentPoint.Position.X- LastPoint.X);
                swapChainRenderer.PointChange.Y = (args.CurrentPoint.Position.Y- LastPoint.Y);

            }
            LastPoint = new Vector2();
            LastPoint.X = (float)args.CurrentPoint.Position.X;
            LastPoint.Y = (float)args.CurrentPoint.Position.Y;
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
