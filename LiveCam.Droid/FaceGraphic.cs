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
            MainActivity.facesList.Add(id);
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
            canvas.DrawRect(left - leftCenterX , top, right - leftCenterX , bottom, mBoxPaint);
            //Правая
            //canvas.DrawRect(left + SCREEN_WIDTH / 4.0f, top, right + SCREEN_WIDTH / 4.0f, bottom, mBoxPaint);
            canvas.DrawRect(left + leftCenterX , top, right + leftCenterX , bottom, mBoxPaint);
            //Id для теста (левый и правый глаз)
            //canvas.DrawText(face.Id.ToString(), right - leftCenterX + 40, bottom, mIdPaint);
            //canvas.DrawText(face.Id.ToString(), right + leftCenterX + 40, bottom, mIdPaint);
            //----------test text--------------

            //canvas.DrawText("Имя: Иван", right - leftCenterX + 30, bottom - 190, mIdPaint);
            //canvas.DrawText("Имя: Иван", right + leftCenterX + 30, bottom - 190, mIdPaint);

            //---------------------------------
            //Проверка принадлежности данных
            //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            //Сюда добавить перебор по словарю data
            if (MainActivity.data.Count != 0)//&& MainActivity.recievedJson.Id == face.Id)
            {
                if (MainActivity.data.Count > 1)
                {


                    Console.WriteLine("data line = name = " + MainActivity.data[0]["name"] + ", x = " + MainActivity.data[0]["x"] + ", y = " + MainActivity.data[0]["y"] + "name = " + MainActivity.data[1]["name"] + ", x = " + MainActivity.data[1]["x"] + ", y = " + MainActivity.data[1]["y"]);
                }
                canvas.DrawText(MainActivity.data[0]["name"], right - leftCenterX -200, bottom + 50 , mIdPaint);
                canvas.DrawText(MainActivity.data[0]["name"], right + leftCenterX -200, bottom + 50, mIdPaint);
                //Console.WriteLine("Ответ: " + MainActivity.recievedJson.Name);
                //canvas.DrawText(MainActivity.recievedJson.Name, right - leftCenterX + 30, top / 2, mIdPaint);
                //canvas.DrawText(MainActivity.recievedJson.Name, right + leftCenterX + 30, top / 2, mIdPaint);
            }
        }
    }
}