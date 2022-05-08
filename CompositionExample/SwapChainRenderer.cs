// Copyright (c) Microsoft Corporation. All rights reserved.
//
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Composition;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Composition;

namespace CompositionExample
{
    class SwapChainRenderer : IDisposable
    {
        Compositor compositor;
        CanvasSwapChain swapChain;
        SpriteVisual swapChainVisual;

        CanvasSwapChain selectedSwapChain;
        SpriteVisual selectedSwapChainVisual;

        CancellationTokenSource drawLoopCancellationTokenSource;

        Dictionary<string, List<string>> keyValuePairs;
        Dictionary<string, Point> pointMap;
        private Dictionary<string, bool> CapturedRelationships;
        private Dictionary<string, int> CapturedEntities;

        private Dictionary<string, Rect> VisibleRects;

        private Graph<Entity> graph;
        private GraphNode<Entity> GraphEntity;

        private Size EntitySize;

        //int drawCount;
        int deviceCount;

        volatile string entitySelected;

        volatile Windows.UI.Input.PointerPoint CurrentPointer;
        public Point PointChange;
        public volatile bool renderUpdate=true;
        //private Point check;
        public bool CheckEntitySelected(Windows.UI.Input.PointerPoint pointerPoint, Vector2 SizeOffset)
        {
            entitySelected = null;
            //SizeOffset.X -= this.Visual.Parent.Offset.X;
            //SizeOffset.Y -= this.Visual.Parent.Offset.Y;
            
            if (VisibleRects !=null && this.Visual.Parent!=null)
                foreach(var rect in VisibleRects)
                {
                    //SizeOffset =(SizeOffset / this.Visual.Size);
                    
                    Point point = new Point();
                    point.X = (pointerPoint.Position.X/ (SizeOffset.X)) - (this.Visual.Parent.Offset.X/ (SizeOffset.X));
                    point.Y = (pointerPoint.Position.Y/(SizeOffset.Y) ) - (this.Visual.Parent.Offset.Y/ (SizeOffset.Y) );

                    if (rect.Value.Contains(point))
                    {
                        CurrentPointer = pointerPoint;
                        entitySelected = rect.Key;
                        GraphEntity = graph.BFS(entitySelected);
                        renderUpdate = false;
                        return true;
                    }
                       
                }
            return false;
        }


        public void ResetSelection()
        {
            CurrentPointer = null;
            entitySelected=null;
            renderUpdate = true;
        }

        
        
        public Windows.UI.Input.PointerPoint ClickedArea { get; set; }

        public Visual Visual { get { return swapChainVisual; } }
        public Visual SelectedVisual { get { return selectedSwapChainVisual; } }
        public Size Size
        {
            get
            {
                if (swapChain == null)
                    return new Size(0, 0);

                return swapChain.Size;
            }
        }
        
        public SwapChainRenderer(Compositor compositor, Size EntitySize)
        {
            this.EntitySize = EntitySize;
            VisibleRects = new Dictionary<string, Rect>();
            this.compositor = compositor;
            swapChainVisual = compositor.CreateSpriteVisual();
            selectedSwapChainVisual = compositor.CreateSpriteVisual();
           
        }

        public void Dispose()
        {
            drawLoopCancellationTokenSource?.Cancel();
            swapChain?.Dispose();
        }

        public void SetDevice(CanvasDevice device, Size windowSize)
        {
            ++deviceCount;

            drawLoopCancellationTokenSource?.Cancel();
            Size size = SetFromGraph();
            swapChain = new CanvasSwapChain(device, (float)size.Width, (float)size.Height, 96);
            swapChainVisual.Brush = compositor.CreateSurfaceBrush(CanvasComposition.CreateCompositionSurfaceForSwapChain(compositor, swapChain));
            selectedSwapChain = new CanvasSwapChain(device, (float)size.Width, (float)size.Height, 96);
            selectedSwapChainVisual.Brush = compositor.CreateSurfaceBrush(CanvasComposition.CreateCompositionSurfaceForSwapChain(compositor, selectedSwapChain));
            drawLoopCancellationTokenSource = new CancellationTokenSource();
            Task.Factory.StartNew(
                DrawLoop,
                drawLoopCancellationTokenSource.Token,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
        }

        private Size SetFromGraph()
        {
            OutputClipboardText();
            CapturedRelationships = new Dictionary<string, bool>();
            int MaxCount = 0;

            CapturedEntities = new Dictionary<string, int>();

            foreach (List<GraphNode<Entity>> nodes in graph.ConnectedNodes)
            {

                if (nodes == null)
                    break;

                if (!CapturedEntities.ContainsKey(nodes[0].Value.Description))
                {
                    CapturedEntities.Add(nodes[0].Value.Description, 2);
                }
                else
                    CapturedEntities[nodes[0].Value.Description] += 2;
                if (CapturedEntities[nodes[0].Value.Description] > MaxCount)
                    MaxCount = CapturedEntities[nodes[0].Value.Description];
            }
            foreach (List<GraphNode<Entity>> nodes in graph.ConnectedNodes)
            {

                if (nodes == null)
                    break;

                foreach (Relationship rel in nodes[0].Value.Relationships.Values)
                {

                    if (!CapturedRelationships.ContainsKey(rel.ToEntity.Name + rel.FromEntity.Name))
                    {
                        CapturedRelationships.Add(rel.ToEntity.Name + rel.FromEntity.Name, true);
                    }

                }

            }
            //Setting Visible rects dictionary
            foreach (List<GraphNode<Entity>> ent in graph.ConnectedNodes)
            {
                if (ent != null && ent[0].Name!="")
                {
                    GraphNode<Entity> e = ent[0];
                    Rect TempShape = new Rect { Height = this.EntitySize.Height, Width = this.EntitySize.Width, X = e.xPoint * 1.25, Y = e.yPoint * 1.25 };
                    VisibleRects.Add(e.Name, new Rect(TempShape.X,TempShape.Y, TempShape.Width, TempShape.Height));

                    
                }

            }

            return new Size((CapturedEntities.Count * (EntitySize.Width * 1.25) + EntitySize.Width), (MaxCount * (EntitySize.Height * 1.25)) + EntitySize.Height);

        }


        void DrawLoop()
        {
            var canceled = drawLoopCancellationTokenSource.Token;

            try
            {
                // Tracking the previous pause state lets us draw once even after becoming paused,
                // so the label text can change to indicate the paused state.
                //bool wasPaused = false;
                //DrawSwapChain(swapChain);
                while (!canceled.IsCancellationRequested)
                {


                    if (entitySelected!=null)
                    {

                        //VisibleRects[entitySelected].X = CurrentPointer.Position.X;
                       // Point check = new Point(Visual.Offset.X + (CurrentPointer.Position.X - Visual.Offset.X),
                       //Visual.Offset.Y + (CurrentPointer.Position.Y - Visual.Offset.Y));
                        VisibleRects[entitySelected] = new Rect(
                            PointChange.X + VisibleRects[entitySelected].X, PointChange.Y + VisibleRects[entitySelected].Y,
                            VisibleRects[entitySelected].Width, VisibleRects[entitySelected].Height);
                        DrawSwapChain(selectedSwapChain,GraphEntity);
                    }
                    else if(renderUpdate)
                    {
                        DrawSwapChain(swapChain);
                        renderUpdate = false;
                    }



                    //wasPaused = isPaused;
                }

                //swapChain.Dispose();
                //selectedSwapChain.Dispose();
            }
            catch (Exception e) when (swapChain.Device.IsDeviceLost(e.HResult))
            {
                swapChain.Device.RaiseDeviceLost();
            }
        }

        //void DrawSwapChain(CanvasSwapChain swapChain, bool isPaused)
        //{
        //    ++drawCount;

        //    using (var ds = swapChain.CreateDrawingSession(Colors.Transparent))
        //    {

        //        var size = swapChain.Size.ToVector2();
        //        var radius = (Math.Min(size.X, size.Y) / 2.0f) - 4.0f;

        //        var center = size / 2;

        //        ds.FillCircle(center, radius, Colors.LightGoldenrodYellow);
        //        ds.DrawCircle(center, radius, Colors.LightGray);

        //        double mu = (-drawCount / 50.0f);

        //        for (int i =0; i < 16; ++i)
        //        {
        //            double a = mu + (i / 16.0) * Math.PI * 2;
        //            var x = (float)Math.Sin(a);
        //            var y = (float)Math.Cos(a);
        //            ds.DrawLine(center, center + new Vector2(x, y) * radius, Colors.Black, 5);
        //        }

        //        var rectLength = Math.Sqrt(radius * radius * 2);

        //        ds.FillCircle(center, (float)rectLength / 2, Colors.LightGoldenrodYellow);

        //        var rect = new Rect(center.X - rectLength / 2, center.Y - rectLength / 2, rectLength, rectLength);

        //        ds.DrawText("This is a swap chain",
        //            rect,
        //            Colors.Black,
        //            new CanvasTextFormat()
        //            {
        //                FontFamily = "Comic Sans MS",
        //                FontSize = 24,
        //                VerticalAlignment = CanvasVerticalAlignment.Center,
        //                HorizontalAlignment = CanvasHorizontalAlignment.Center,
        //                WordWrapping = CanvasWordWrapping.WholeWord,
        //            });

        //        var label = string.Format("Draws: {0}\nDevices: {1}\nTap to {2}", drawCount, deviceCount, isPaused ? "unpause" : "pause");

        //        ds.DrawText(label, rect, Colors.Black, new CanvasTextFormat()
        //        {
        //            FontSize = 10,
        //            VerticalAlignment = CanvasVerticalAlignment.Bottom,
        //            HorizontalAlignment = CanvasHorizontalAlignment.Center
        //        });
        //    }

        //    swapChain.Present();
        //}

        void DrawSwapChain(CanvasSwapChain swapChain)
        {
            using (var ds = swapChain.CreateDrawingSession(Colors.Transparent))
            {
                foreach (List<GraphNode<Entity>> ent in graph.ConnectedNodes)
                {
                    if (ent != null && ent[0].Name!="")
                    {
                        GraphNode<Entity> e = ent[0];
                        Rect TempShape = VisibleRects[e.Name];
                        ds.FillRoundedRectangle(TempShape, 10, 10, Colors.LightBlue);
                        ds.DrawRoundedRectangle(TempShape, 10, 10, Colors.Gray, 2);
                        //VisibleRects.Add(e.Name, TempShape);
                        if (TempShape.Y - TempShape.Height < TempShape.Height)
                            ds.DrawText(e.Value.Description, new Vector2 { X = (float)((float)TempShape.X), Y = (float)(TempShape.Y - e.yPoint) }, Colors.Black);


                        ds.DrawText(e.Name, TempShape, Colors.Black, new CanvasTextFormat()
                        {
                            FontFamily = "Arial",
                            FontSize = 12,
                            WordWrapping = CanvasWordWrapping.WholeWord,
                            VerticalAlignment = CanvasVerticalAlignment.Center,
                            HorizontalAlignment = CanvasHorizontalAlignment.Center
                        });
                    }

                }
                foreach (List<GraphNode<Entity>> ent in graph.ConnectedNodes)
                {
                    if (ent != null && ent[0].Name!="")
                    {
                        GraphNode<Entity> e = ent[0];
                        Rect TempShape = VisibleRects[e.Name];

                        foreach (GraphNode<Entity> link in graph.ConnectedNodes[e.NodeIndex])
                        {


                            //GraphNode<Entity> link = graph.BFS(rl.ToEntity.Name);

                            Rect ToShape = VisibleRects[link.Name];
                            Point To = this.To(new Entity { EntityShape = ToShape }, new Entity { EntityShape = TempShape });
                            Point From = this.From(new Entity { EntityShape = ToShape }, new Entity { EntityShape = TempShape });

                            ds.DrawLine(new Vector2((float)From.X, (float)From.Y), new Vector2((float)To.X, (float)To.Y), Colors.Black);
                            //CanvasPathBuilder canvasPathBuilder = new CanvasPathBuilder(this.);

                        }
                    }

                }

            }
            swapChain.Present();
        }

        void DrawSwapChain(CanvasSwapChain swapChain, GraphNode<Entity> e)
        {
            if(e!=null)
            using (var ds = selectedSwapChain.CreateDrawingSession(Colors.Transparent))
            {
                //foreach (List<GraphNode<Entity>> ent in )
                //{
                //    if (ent != null)
                //    {
                //GraphNode<Entity> e = graph.BFS(entitySelected);
                
                    Rect TempShape = VisibleRects[e.Name];
                    ds.FillRoundedRectangle(TempShape, 10, 10, Colors.Transparent);
                    ds.DrawRoundedRectangle(TempShape, 10, 10, Colors.Gray, 2);
                    //VisibleRects.Add(e.Name, TempShape);
                    if (TempShape.Y - TempShape.Height < TempShape.Height)
                        ds.DrawText(e.Value.Description, new Vector2 { X = (float)((float)TempShape.X), Y = (float)(TempShape.Y - e.yPoint) }, Colors.Black);


                    ds.DrawText(e.Name, TempShape, Colors.Gray, new CanvasTextFormat()
                    {
                        FontFamily = "Arial",
                        FontSize = 12,
                        WordWrapping = CanvasWordWrapping.WholeWord,
                        VerticalAlignment = CanvasVerticalAlignment.Center,
                        HorizontalAlignment = CanvasHorizontalAlignment.Center
                    });

                foreach (GraphNode<Entity> link in graph.ConnectedNodes[e.NodeIndex])
                {

                    // = graph.BFS(rl.ToEntity.Name);

                    Rect ToShape = VisibleRects[link.Name];
                    Point To = this.To(new Entity { EntityShape = ToShape }, new Entity { EntityShape = TempShape });
                    Point From = this.From(new Entity { EntityShape = ToShape }, new Entity { EntityShape = TempShape });

                    ds.DrawLine(new Vector2((float)From.X, (float)From.Y), new Vector2((float)To.X, (float)To.Y), Colors.Black);
                    //CanvasPathBuilder canvasPathBuilder = new CanvasPathBuilder(this.);

                }

            }
            selectedSwapChain.Present();
        }
        public void OutputClipboardText()
        {
            try
            {


                graph = new Graph<Entity>(1000);
                pointMap = new Dictionary<string, Point>();
                List<string> props = new List<string>();
                keyValuePairs = new Dictionary<string, List<string>>();


                DataPackageView dataPackageView = Clipboard.GetContent();
                if (dataPackageView.Contains(StandardDataFormats.Text))
                {
                    var task = Task.Run(async () => await dataPackageView.GetTextAsync());
                    var result = task.Result;

                    string text = result.ToString();
                    string[] pastedRows = Regex.Split(text.TrimEnd("\r\n".ToCharArray()), "\r\n");


                    int DescriptionCount = 1;
                    foreach (string cell in pastedRows[0].Split(new char[] { '\t' }))
                    {

                        keyValuePairs.Add(cell, new List<string>());
                        props.Add(cell);
                        pointMap.Add(cell, new Point(DescriptionCount, 0));
                        DescriptionCount += 1;
                    }



                    for (int r = 1; r < pastedRows.Length; r++)
                    {
                        string[] pastedRowCells = pastedRows[r].Split(new char[] { '\t' });

                        GraphNode<Entity> tempNode = null;
                        for (int i = 0; i < pastedRowCells.Length; i++)
                        {
                            GraphNode<Entity> Link = graph.BFS(pastedRowCells[i]);
                            if (Link == null)
                            {
                                Point temp = new Point(pointMap[props[i]].X, pointMap[props[i]].Y + 1);
                                if (!pointMap.ContainsKey(pastedRowCells[i]))
                                    pointMap.Add(pastedRowCells[i], new Point(
                                        (pointMap[props[i]].X) + ((pointMap[props[i]].X) * .10),
                                        pointMap[props[i]].Y + ((pointMap[props[i]].Y) * .10)));
                                pointMap[props[i]] = temp;

                                Link = graph.ConnectedNodes[graph.AddNode(pastedRowCells[i],
                                    new Entity
                                    {
                                        Name = pastedRowCells[i],
                                        EntityColor = Colors.Black,
                                        Description = props[i],
                                        EntityShape = new Rect(
                                            new Point(
                                                pointMap[pastedRowCells[i]].X * EntitySize.Width,
                                                pointMap[pastedRowCells[i]].Y * EntitySize.Height),
                                                EntitySize)
                                    })][0];
                                graph.ConnectedNodes[Link.NodeIndex][0].xPoint = (int)((int)temp.X * EntitySize.Width);
                                graph.ConnectedNodes[Link.NodeIndex][0].yPoint = (int)((int)temp.Y * EntitySize.Height);
                            }

                            keyValuePairs[props[i]].Add(pastedRowCells[i]);
                            if (tempNode != null)
                            {

                                if (!tempNode.Value.Relationships.ContainsKey(Link.Name))
                                {
                                    //pointMap.Add(Link.Name + tempNode.Name, new Point(,) );
                                    graph.ConnectDirectToAndFromNode(Link.Name, tempNode.Name, 1);
                                    tempNode.Value.Relationships.Add(Link.Name, new Relationship
                                    {
                                        Color = Colors.Wheat,
                                        FromEntity = graph.ConnectedNodes[tempNode.NodeIndex][0].Value,
                                        ToEntity = graph.ConnectedNodes[Link.NodeIndex][0].Value
                                    });
                                }
                            }

                            tempNode = Link;
                        }

                    }

                }
            }
            catch (Exception ex)
            {
                graph = new Graph<Entity>(1);
                graph.AddNode(ex.Message, new Entity { Description = "ERROR", Name = ex.Message, EntityShape = new Rect(0, 0, 400, 400) });

            }
        }



        private Point To(Entity ToEntity, Entity FromEntity)
        {
            Point point = new Point();
            if (ToEntity.EntityShape.Top >= FromEntity.EntityShape.Top)
            {
                //To entity is further down, set point Y to reflect mid point to be the top point of the rect
                point.Y = ToEntity.EntityShape.Top;
            }
            else
            {
                //To entity is above, set the point to the bottom of the entity
                point.Y = ToEntity.EntityShape.Bottom;
            }

            if (ToEntity.EntityShape.Left >= FromEntity.EntityShape.Left)
            {
                //To entity is further right. set the x point to the left of the to entity
                point.X = ToEntity.EntityShape.Left;

            }
            else
            {
                point.X = ToEntity.EntityShape.Right;
            }
            return point;
        }
        private Point From(Entity ToEntity, Entity FromEntity)
        {
            Point point = new Point();
            if (FromEntity.EntityShape.Top >= ToEntity.EntityShape.Top)
            {
                //To entity is further down, set point Y to reflect mid point to be the top point of the rect
                point.Y = Math.Max(FromEntity.EntityShape.Top, 10);
            }
            else
            {
                //To entity is above, set the point to the bottom of the entity
                point.Y = Math.Max(FromEntity.EntityShape.Bottom, 10);

            }

            if (FromEntity.EntityShape.Left >= ToEntity.EntityShape.Left)
            {
                //To entity is further right. set the x point to the left of the to entity
                point.X = Math.Max(FromEntity.EntityShape.Left, 10);

            }
            else
            {
                point.X = Math.Max(FromEntity.EntityShape.Right, 10);
            }

            return point;
        }
    }
}
