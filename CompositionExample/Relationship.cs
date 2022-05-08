using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI;

namespace CompositionExample
{
    public class Relationship
    {
        public Entity ToEntity { get; set; }
        public Entity FromEntity { get; set; }
        public Color Color { get; set; }
        private Size size { get; set; }

        public Relationship()
        {
            size = new Size();
        }
        public Point To()
        {
            Point point = new Point();
            if (ToEntity.EntityShape.Top>=FromEntity.EntityShape.Top)
            {
                //To entity is further down, set point Y to reflect mid point to be the top point of the rect
                if (ToEntity.EntityShape.Top >= FromEntity.EntityShape.Top)
                        
                if(ToEntity.EntityShape.Bottom >= FromEntity.EntityShape.Top)
                {
                        //Bottom is above entity
                        point.Y = ToEntity.EntityShape.Top;
                        
                }
                {
                    //Connect Mid Y
                    point.Y = ToEntity.EntityShape.Top+(ToEntity.EntityShape.Height/2);
                }
            }
            else
            {
                //To entity is above, set the point to the bottom of the entity
                point.Y = ToEntity.EntityShape.Bottom;
            }

            if (ToEntity.EntityShape.Left >=FromEntity.EntityShape.Left)
            {
                
                if(ToEntity.EntityShape.Left+ ToEntity.EntityShape.Width >= FromEntity.EntityShape.Left)
                    //To entity is further right. set the x point to the left of the to entity
                    point.X = ToEntity.EntityShape.Left;
                else
                {
                    //Connect center
                    point.X = ToEntity.EntityShape.Left+(ToEntity.EntityShape.Width/2);

                }
                

            } else
            {
                point.X = ToEntity.EntityShape.Right;
            }
            return point;
        }
        public Point From()
        {
            //Point point = new Point();
            //if (FromEntity.EntityShape.Top >= ToEntity.EntityShape.Top)
            //{
            //    //To entity is further down, set point Y to reflect mid point to be the top point of the rect
            //    point.Y = Math.Max(FromEntity.EntityShape.Top,10);
            //}
            //else
            //{
            //    //To entity is above, set the point to the bottom of the entity
            //    point.Y = Math.Max(FromEntity.EntityShape.Bottom,10);

            //}

            //if (FromEntity.EntityShape.Left >= ToEntity.EntityShape.Left)
            //{
            //    //To entity is further right. set the x point to the left of the to entity
            //    point.X = Math.Max(FromEntity.EntityShape.Left,10);

            //}
            //else
            //{
            //    point.X = Math.Max(FromEntity.EntityShape.Right,10);
            //}

            //return point;
            Point point = new Point();
            if (FromEntity.EntityShape.Top >= ToEntity.EntityShape.Top)
            {
                //To entity is further down, set point Y to reflect mid point to be the top point of the rect
                if (FromEntity.EntityShape.Top >= ToEntity.EntityShape.Top)

                    if (FromEntity.EntityShape.Bottom >= ToEntity.EntityShape.Top)
                    {
                        //Bottom is above entity
                        point.Y = FromEntity.EntityShape.Top;

                    }
                {
                    //Connect Mid Y
                    point.Y = FromEntity.EntityShape.Top + (FromEntity.EntityShape.Height / 2);
                }
            }
            else
            {
                //To entity is above, set the point to the bottom of the entity
                point.Y = FromEntity.EntityShape.Bottom;
            }

            if (FromEntity.EntityShape.Left >= ToEntity.EntityShape.Left)
            {

                if (FromEntity.EntityShape.Left + ToEntity.EntityShape.Width >= ToEntity.EntityShape.Left)
                    //To entity is further right. set the x point to the left of the to entity
                    point.X = FromEntity.EntityShape.Left;
                else
                {
                    //Connect center
                    point.X = FromEntity.EntityShape.Left + (ToEntity.EntityShape.Width / 2);

                }


            }
            else
            {
                point.X = FromEntity.EntityShape.Right;
            }
            return point;
        }

        public Size Size()
        {
            Size size1 = new Size();
            //ToEntity top is further down
            Point From = this.From();
            Point To = this.To();
            size1.Width = Math.Max(From.X, To.X) - Math.Min(From.X, To.X);
            size1.Height = Math.Max(From.Y, To.Y) - Math.Min(From.Y, To.Y);
            return size1;
            
        }

    }

    
}