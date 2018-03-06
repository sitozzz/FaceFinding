using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Gms.Vision.Faces;
using System.Threading.Tasks;

namespace LiveCam.Droid
{
    public class FaceGraphic : Graphic
    {
        //Угол обзора камеры
        private static readonly float FOV = 90.0f;
        private static readonly float SOV = 40.0f;

        private static readonly float FACE_POSITION_RADIUS = 10.0f;
        private static readonly float ID_TEXT_SIZE = 40.0f;
        private static readonly float ID_Y_OFFSET = 50.0f;
        private static readonly float ID_X_OFFSET = -50.0f;
        private static readonly float BOX_STROKE_WIDTH = 5.0f;
        //Размеры холста
        private static readonly float SCREEN_WIDTH = MainActivity.width;//1280.0f;
        private static readonly float SCREEN_HEIGHT = MainActivity.height;//720.0f;
        //Цвета для рамок
        private static Color[] COLOR_CHOICES = {
        Color.Blue,
        Color.Cyan,
        Color.Green,
        Color.Magenta,
        Color.Red,
        Color.White,
        Color.Yellow
    };

        private static int mCurrentColorIndex = 0;

        private Paint mFacePositionPaint;
        private Paint mIdPaint;
        private Paint mBoxPaint;

        private volatile Face mFace;
        private int mFaceId;
        private float mFaceHappiness;

        public static Dictionary<int, string> detectedNames;
        public static bool[] drawable;
        public FaceGraphic(GraphicOverlay overlay) : base(overlay)
        {
            mCurrentColorIndex = (mCurrentColorIndex + 1) % COLOR_CHOICES.Length;
            var selectedColor = COLOR_CHOICES[mCurrentColorIndex];

            mFacePositionPaint = new Paint()
            {
                Color = selectedColor
            };
            mIdPaint = new Paint()
            {
                Color = selectedColor,
                TextSize = ID_TEXT_SIZE
            };
            mBoxPaint = new Paint()
            {
                Color = selectedColor
            };
            mBoxPaint.SetStyle(Paint.Style.Stroke);
            mBoxPaint.StrokeWidth = BOX_STROKE_WIDTH;
        }
        public void SetId(int id)
        {
            mFaceId = id;
            //MainActivity.facesList.Add(id);
        }


        /**
         * Updates the face instance from the detection of the most recent frame.  Invalidates the
         * relevant portions of the overlay to trigger a redraw.
         */
        public void UpdateFace(Face face)
        {
            mFace = face;
            PostInvalidate();
        }

        public override void Draw(Canvas canvas)
        {
            
            //Определяем лицо
            Face face = mFace;
            if (face == null)
            {
                return;
            }

            // Draws a circle at the position of the detected face, with the face's track id below.
            float x = TranslateX(face.Position.X + face.Width / 2);
            float y = TranslateY(face.Position.Y + face.Height / 2);
            //Отрисовка точки в центре квадрата
            //canvas.DrawCircle(x, y, FACE_POSITION_RADIUS, mFacePositionPaint);

            //HACK: Demo only

            //if (!string.IsNullOrEmpty(MainActivity.GreetingsText))
            //{
            //    canvas.DrawText(MainActivity.GreetingsText, x + ID_X_OFFSET, y + ID_Y_OFFSET, mIdPaint);
            //}


            ////Левая
            //canvas.DrawRect(left - leftCenterX, top, right - leftCenterX, bottom, mBoxPaint);
            ////Правая
            //canvas.DrawRect(left + leftCenterX, top, right + leftCenterX, bottom, mBoxPaint);

            //Отображение текста
            //Дописать правильное отображение текста по id!

            //canvas.DrawText("happiness: " + Math.Round(face.IsSmilingProbability, 2).ToString(), x - ID_X_OFFSET, y - ID_Y_OFFSET, mIdPaint);
            //canvas.DrawText("right eye: " + Math.Round(face.IsRightEyeOpenProbability, 2).ToString(), x + ID_X_OFFSET * 2, y + ID_Y_OFFSET * 2, mIdPaint);
            //canvas.DrawText("left eye: " + Math.Round(face.IsLeftEyeOpenProbability, 2).ToString(), x - ID_X_OFFSET * 2, y - ID_Y_OFFSET * 2, mIdPaint);
            //canvas.DrawText("pos = " + face.Position, x - ID_X_OFFSET, y - ID_Y_OFFSET, mIdPaint);
            //canvas.DrawText("x: " + x.ToString(), x - ID_X_OFFSET * 2, y - ID_Y_OFFSET * 2, mIdPaint);
            //canvas.DrawText("y: " + y.ToString(), x + ID_X_OFFSET * 2, y + ID_Y_OFFSET * 2, mIdPaint);
            // Draws a bounding box around the face.

            float xOffset = ScaleX(face.Width / 2.0f);
            float yOffset = ScaleY(face.Height / 2.0f);
            //Координаты квадрата
            float left = x - xOffset;
            float top = y - yOffset;
            float right = x + xOffset;
            float bottom = y + yOffset;
            //-----------------------------------------------------------------------
            //Возможно стоит использовать face.Position.X

            //Погрешность установки телефона в шлем
            float dx = 0.0f;
            float dy = 0.0f;

            //Угловые координаты
            float angularCoordinateX = (x - SCREEN_WIDTH / 2) / FOV;
            float angularCoordinateY = (y - SCREEN_HEIGHT / 2) / FOV;

            //Центр левого глаза
            float leftCenterX = SCREEN_WIDTH / 4;
            float leftCenterY = SCREEN_HEIGHT / 2;

            //Центр правого глаза
            float rightCenterX = (SCREEN_WIDTH * 3 / 4);
            float rightCenterY = SCREEN_HEIGHT / 2;

            //Координаты левой рамки
            float leftRectX = leftCenterX - dx + (x - SCREEN_WIDTH / 2) * (FOV / SCREEN_HEIGHT) * SCREEN_HEIGHT / SOV;
            float leftRectY = leftCenterY + dy + (y * SCREEN_HEIGHT / 2) * (FOV / SCREEN_HEIGHT) * SCREEN_HEIGHT / SOV;

            //Координаты правой рамки
            float rightRectX = rightCenterX + dx + (x - SCREEN_WIDTH / 2) * (FOV / SCREEN_HEIGHT) * SCREEN_HEIGHT / SOV;
            float rightRectY = rightCenterY + dy + (y - SCREEN_HEIGHT / 2) * (FOV / SCREEN_HEIGHT) * SCREEN_HEIGHT / SOV;
            //------------------------------------------------------------------------
            //Отрисовка рамок
            //Левая
            //canvas.DrawRect(left - SCREEN_WIDTH / 4.0f, top, right - SCREEN_WIDTH / 4.0f, bottom, mBoxPaint);
            canvas.DrawRect(left - leftCenterX + 300, top, right - leftCenterX + 300, bottom, mBoxPaint);
            //Правая
            //canvas.DrawRect(left + SCREEN_WIDTH / 4.0f, top, right + SCREEN_WIDTH / 4.0f, bottom, mBoxPaint);
            canvas.DrawRect(left + leftCenterX - 300, top, right + leftCenterX - 300, bottom, mBoxPaint);
            //Id для теста (левый и правый глаз)
            //canvas.DrawText(face.Id.ToString(), right - leftCenterX + 40, bottom, mIdPaint);
            //canvas.DrawText(face.Id.ToString(), right + leftCenterX + 40, bottom, mIdPaint);

           

            if (MainActivity.facesList == null)
            {
                MainActivity.facesList = new List<int>();
                MainActivity.faceid_id = new List<int>();

            }

            if (MainActivity.data != null && MainActivity.data.Count != 0)
            {
                //for (int i = 0; i == MainActivity.data.Count; i++)
                //{
                //    MainActivity.data[i]["x" + i];
                //}
                //Console.WriteLine("Count = " + MainActivity.data.Count);

                if (FaceGraphic.drawable == null)
                {
                    FaceGraphic.drawable = new bool[MainActivity.data.Count];
                }
                
                //facesList.Count > data.Count!!!!
                if (MainActivity.facesList.Count > MainActivity.data.Count)
                {
                    //foreach (var item in MainActivity.facesList)
                    //{
                    //    Console.WriteLine("ItemInFacecList = " + item);
                    //}
                    MainActivity.facesList.RemoveAt(0);
                }

                if (GraphicFaceTracker.checkId(MainActivity.facesList, face.Id) == false)
                {
                    Console.WriteLine("Прошел чек");
                    for (int i = 0; i < MainActivity.data.Count; ++i)
                    {
                        drawable[i] = false;

                        var x0 = Convert.ToInt32(MainActivity.data[i]["x"]);
                        var y0 = Convert.ToInt32(MainActivity.data[i]["y"]);
                        //Центр лица
                        var xCenter = Convert.ToInt32(MainActivity.data[i]["x"]) + (Convert.ToInt32(MainActivity.data[i]["width"]) / 2) - 100;
                        var yCenter = Convert.ToInt32(MainActivity.data[i]["y"]) - (Convert.ToInt32(MainActivity.data[i]["height"]) / 2);
                        Console.WriteLine("Check data: x vision = " + face.Position.X + ", x calc = " + xCenter + "; y vision = " + face.Position.Y + ", y calc = " + yCenter);
                        //Console.WriteLine(GraphicFaceTracker.response_id.ToString() + ":" + i.ToString() + ": x = " + x0 + ", y = " + y0 + "; xface = " + left + ", yface = " + top);
                        // Console.WriteLine("width = " + MainActivity.data[i]["width"] + ", y = " + MainActivity.data[i]["height"] + "; widthface = " + right + ", heightface = " + bottom);

                        //if ((Math.Abs(left - x0) <= 250) && (Math.Abs(top - y0) <= 250))
                        if ((Math.Abs(face.Position.X - xCenter) <= 150) && (Math.Abs(face.Position.Y - yCenter) <= 150))
                        {

                            MainActivity.facesList.Add(face.Id);
                            //Console.WriteLine("Added " + i + " element to facesList" + MainActivity.facesList[i]);
                            MainActivity.faceid_id.Add(i);
                            drawable[i] = true;
                            //MainActivity.facesList.Add(face.Id);
                            //Console.WriteLine("Added new id - " + face.Id + "Count = " + MainActivity.facesList.Count);
                        }

                    }
                    
                }
                
             
                

                if (MainActivity.facesList.Count != 0 && MainActivity.facesList.Count == MainActivity.data.Count)
                {
                    for (int i = 0; i < MainActivity.facesList.Count; ++i)
                    {
                        //if (!drawable[i])
                        //continue;
                        Console.WriteLine("drawable" + i + " = " + drawable[MainActivity.faceid_id[i]] + "; drawable.Length = " + drawable.Length);

                        if (MainActivity.facesList[i] == face.Id )//&& drawable[MainActivity.faceid_id[i]])
                        {
                            Console.WriteLine("dbag face id = " + face.Id);
                            canvas.DrawText(MainActivity.data[i]["name"] + i.ToString() + " face", left - (leftCenterX + 400), bottom + 50, mIdPaint);
                            canvas.DrawText(MainActivity.data[i]["name"] + i.ToString() + " face", left + (leftCenterX + 400), bottom + 50, mIdPaint);
                            //break;
                        }
                    }
                }
            }
        }
    }
}