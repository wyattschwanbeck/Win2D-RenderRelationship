using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Windows.Foundation;
using System.Numerics;
using Windows.UI.Xaml.Shapes;
using Windows.UI;
using Windows.UI.Xaml.Media;
using Windows.UI.Input;
using Windows.ApplicationModel.DataTransfer;
using System.Text.RegularExpressions;
using Windows.UI.Composition;
using Windows.Graphics.DirectX;
using Microsoft.Graphics.Canvas.UI.Composition;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.Geometry;

namespace CompositionExample
{
    class RelationshipHandler
    {

        public SpriteVisual drawingSurfaceVisual;
        public CompositionDrawingSurface drawingSurface;
        private Dictionary<string, List<string>> keyValuePairs;
        private Dictionary<string, Point> pointMap;

        public Visual Visual { get { return drawingSurfaceVisual; } }

        public Size Size { get { return drawingSurface.Size; } }

        private Graph<Entity> graph;

        private Size EntitySize;
        //private CanvasPathBuilder cpb;
        public RelationshipHandler(Compositor compositor, 
            CompositionGraphicsDevice compositionGraphicsDevice, 
            Size entitySize) 
        {
            this.EntitySize = entitySize;
            

            pointMap = new Dictionary<string, Point>();
            drawingSurfaceVisual = compositor.CreateSpriteVisual();
            graph = new Graph<Entity>(100000);
            drawingSurface = compositionGraphicsDevice.CreateDrawingSurface(SetFromGraph(), 
                DirectXPixelFormat.B8G8R8A8UIntNormalized, DirectXAlphaMode.Premultiplied);
           
            drawingSurfaceVisual.Brush = compositor.CreateSurfaceBrush(drawingSurface);
                
            compositionGraphicsDevice.RenderingDeviceReplaced += CompositionGraphicsDevice_RenderingDeviceReplaced;
            DrawDrawingSurface();
        }
        private Dictionary<string, bool> CapturedRelationships;
        private Dictionary<string, int> CapturedEntities;
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
            return new Size((CapturedEntities.Count *(EntitySize.Width*1.25) + EntitySize.Width), (MaxCount * (EntitySize.Height*1.25))+EntitySize.Height);

        }

        void CompositionGraphicsDevice_RenderingDeviceReplaced(CompositionGraphicsDevice sender, RenderingDeviceReplacedEventArgs args)
        {
            DrawDrawingSurface();
        }

        void DrawDrawingSurface()
        {
            using (var ds = CanvasComposition.CreateDrawingSession(drawingSurface))
            {
                Dictionary<string, Rect> VisibleRects = new Dictionary<string, Rect>();

                foreach (List<GraphNode<Entity>> ent in graph.ConnectedNodes)
                {
                    if (ent != null)
                    {
                        GraphNode<Entity> e = ent[0];
                        Rect TempShape = new Rect { Height = this.EntitySize.Height, Width = this.EntitySize.Width, X = e.xPoint * 1.25, Y = e.yPoint * 1.25 };
                        ds.FillRoundedRectangle(TempShape, 10, 10, Colors.LightBlue);
                        ds.DrawRoundedRectangle(TempShape, 10, 10, Colors.Gray, 2);
                        VisibleRects.Add(e.Name, TempShape);
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
                    if (ent != null)
                    {
                        GraphNode<Entity> e = ent[0];
                        Rect TempShape = VisibleRects[e.Name];

                        foreach (Relationship rl in e.Value.Relationships.Values)
                        {

                            GraphNode<Entity> link = graph.BFS(rl.ToEntity.Name);

                            Rect ToShape = VisibleRects[rl.ToEntity.Name];
                            Point To = this.To(new Entity { EntityShape = ToShape }, new Entity { EntityShape = TempShape });
                            Point From = this.From(new Entity { EntityShape = ToShape }, new Entity { EntityShape = TempShape });

                            ds.DrawLine(new Vector2((float)From.X, (float)From.Y), new Vector2((float)To.X, (float)To.Y), Colors.Black);
                            //CanvasPathBuilder canvasPathBuilder = new CanvasPathBuilder(this.);

                        }
                    }

                }

            }
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

