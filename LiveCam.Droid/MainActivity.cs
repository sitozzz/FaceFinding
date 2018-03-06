using Android.App;
using Android.Widget;
using Android.OS;
using Android.Gms.Vision;
using Android.Support.V4.App;
using Android.Support.V7.App;
using ImageService;
using Android.Util;
using Android;
using Android.Support.Design.Widget;
using Android.Content;
using Android.Gms.Vision.Faces;
using Java.Lang;
using System;
using Android.Runtime;
using static Android.Gms.Vision.MultiProcessor;
using Android.Content.PM;
using Android.Gms.Common;
using LiveCam.Shared;
using System.Threading.Tasks;
using ServiceHelpers;
using System.IO;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Runtime.InteropServices;
using Android.Graphics;
using Java.IO;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using Android.Speech.Tts;

namespace LiveCam.Droid
{
    [Activity(Label = "LiveCam.Droid", MainLauncher = true, Icon = "@drawable/icon", Theme = "@style/Theme.AppCompat.NoActionBar", ScreenOrientation = ScreenOrientation.FullSensor)]
    public class MainActivity : AppCompatActivity, IFactory, TextToSpeech.IOnInitListener
    {
        //Список режимов приложения
        public enum AppMode
        {
            Faces,
            Things
        };
        //Переключатель режимов
        public static AppMode currentAppMode;

        private static readonly string TAG = "FaceTracker";

        private CameraSource mCameraSource = null;

        private CameraSourcePreview mPreview;
        private GraphicOverlay mGraphicOverlay;

        //Массив с id лиц
        public static List<int> facesList;
        public static List<int> faceid_id;
        public static float height;
        public static float width;

        //Для получения данных с сервера
        public static RecievedJson recievedJson;

        public static Dictionary<int, Dictionary<string, string>> data;
        public static string GreetingsText
        {
            get;
            set;
        }

        private static readonly int RC_HANDLE_GMS = 9001;
        // permission request codes need to be < 256
        private static readonly int RC_HANDLE_CAMERA_PERM = 2;
        //----------Speech----------------------
        public static TextToSpeech textToSpeech;
        Context context;
        private readonly int MyCheckCode = 101, NeedLang = 103;
        Java.Util.Locale lang;
        protected async override void OnCreate(Bundle bundle)
        {

            base.OnCreate(bundle);
            if (FaceGraphic.detectedNames == null)
            {
                FaceGraphic.detectedNames = new Dictionary<int, string>();
            }

            if (MainActivity.recievedJson == null)
            {
                MainActivity.recievedJson = new RecievedJson();
            }
            if (MainActivity.data == null)
            {
                MainActivity.data = new Dictionary<int, Dictionary<string, string>>();
            }

            //Устанавливаем по умолчанию режим распознавания лиц
            MainActivity.currentAppMode = AppMode.Faces;

            var metrics = Resources.DisplayMetrics;
            MainActivity.width = metrics.WidthPixels;
            MainActivity.height = metrics.HeightPixels;
            //System.Console.WriteLine("width = " + width + ", height = " + height);
            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);

            mPreview = FindViewById<CameraSourcePreview>(Resource.Id.preview);
            mGraphicOverlay = FindViewById<GraphicOverlay>(Resource.Id.faceOverlay);
            //greetingsText = FindViewById<TextView>(Resource.Id.greetingsTextView);

            if (ActivityCompat.CheckSelfPermission(this, Manifest.Permission.Camera) == Permission.Granted)
            {
                CreateCameraSource();
                LiveCamHelper.Init();
                LiveCamHelper.GreetingsCallback = (s) => { RunOnUiThread(() => GreetingsText = s); };
                await LiveCamHelper.RegisterFaces();
            }
            else { RequestCameraPermission(); }

            //Подключаем синтез голоса
            textToSpeech = new TextToSpeech(this, this, "com.google.android.tts");
            var langAvailable = new List<string> { "Default" };
            var localesAvailable = Java.Util.Locale.GetAvailableLocales().ToList();
            foreach (var locale in localesAvailable)
            {
                LanguageAvailableResult res = textToSpeech.IsLanguageAvailable(locale);
                switch (res)
                {
                    case LanguageAvailableResult.Available:
                        langAvailable.Add(locale.DisplayLanguage);
                        break;
                    case LanguageAvailableResult.CountryAvailable:
                        langAvailable.Add(locale.DisplayLanguage);
                        break;
                    case LanguageAvailableResult.CountryVarAvailable:
                        langAvailable.Add(locale.DisplayLanguage);
                        break;
                }

            }
            langAvailable = langAvailable.OrderBy(t => t).Distinct().ToList();

            //var adapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleSpinnerDropDownItem, langAvailable);
            //spinLanguages.Adapter = adapter;

            //lang = Java.Util.Locale.Default;
            lang = new Java.Util.Locale("ru");
            textToSpeech.SetLanguage(lang);

            // set the speed and pitch
            textToSpeech.SetPitch(.65f);
            textToSpeech.SetSpeechRate(.65f);

            //Клик по экрану
            mGraphicOverlay.Click += delegate
            {
                //MainActivity.textToSpeech.Speak("О, здарова!", QueueMode.Flush, null);
            };
        }

        void TextToSpeech.IOnInitListener.OnInit(OperationResult status)
        {
            // if we get an error, default to the default language
            if (status == OperationResult.Error)
                textToSpeech.SetLanguage(Java.Util.Locale.Default);
            // if the listener is ok, set the lang
            if (status == OperationResult.Success)
                textToSpeech.SetLanguage(lang);
        }

        protected override void OnActivityResult(int req, Result res, Intent data)
        {
            if (req == NeedLang)
            {
                // we need a new language installed
                var installTTS = new Intent();
                installTTS.SetAction(TextToSpeech.Engine.ActionInstallTtsData);
                StartActivity(installTTS);
            }
        }

        protected override void OnResume()
        {
            base.OnResume();
            StartCameraSource();
        }

        protected override void OnPause()
        {
            base.OnPause();
            mPreview.Stop();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (mCameraSource != null)
            {
                mCameraSource.Release();
            }
        }

        private void RequestCameraPermission()
        {
            Log.Warn(TAG, "Camera permission is not granted. Requesting permission");

            var permissions = new string[] { Manifest.Permission.Camera };

            if (!ActivityCompat.ShouldShowRequestPermissionRationale(this,
                    Manifest.Permission.Camera))
            {
                ActivityCompat.RequestPermissions(this, permissions, RC_HANDLE_CAMERA_PERM);
                return;
            }

            Snackbar.Make(mGraphicOverlay, Resource.String.permission_camera_rationale,
                    Snackbar.LengthIndefinite)
                    .SetAction(Resource.String.ok, (o) => { ActivityCompat.RequestPermissions(this, permissions, RC_HANDLE_CAMERA_PERM); })
                    .Show();
        }

        /**
 * Creates and starts the camera.  Note that this uses a higher resolution in comparison
 * to other detection examples to enable the barcode detector to detect small barcodes
 * at long distances.
 */
        private void CreateCameraSource()
        {

            var context = Application.Context;
            FaceDetector detector = new FaceDetector.Builder(context)
                    .SetClassificationType(ClassificationType.All)
                    .Build();

            detector.SetProcessor(
                    new MultiProcessor.Builder(this)
                            .Build());

            if (!detector.IsOperational)
            {
                // Note: The first time that an app using face API is installed on a device, GMS will
                // download a native library to the device in order to do detection.  Usually this
                // completes before the app is run for the first time.  But if that download has not yet
                // completed, then the above call will not detect any faces.
                //
                // isOperational() can be used to check if the required native library is currently
                // available.  The detector will automatically become operational once the library
                // download completes on device.
                Log.Warn(TAG, "Face detector dependencies are not yet available.");
            }

            mCameraSource = new CameraSource.Builder(context, detector)
                    .SetRequestedPreviewSize((int)height, (int)width)
                                            .SetFacing(CameraFacing.Back)
                    .SetRequestedFps(24.0f)
                    .Build();

        }

        /**
         * Starts or restarts the camera source, if it exists.  If the camera source doesn't exist yet
         * (e.g., because onResume was called before the camera source was created), this will be called
         * again when the camera source is created.
         */
        private void StartCameraSource()
        {

            // check that the device has play services available.
            int code = GoogleApiAvailability.Instance.IsGooglePlayServicesAvailable(
                    this.ApplicationContext);
            if (code != ConnectionResult.Success)
            {
                Dialog dlg =
                        GoogleApiAvailability.Instance.GetErrorDialog(this, code, RC_HANDLE_GMS);
                dlg.Show();
            }

            if (mCameraSource != null)
            {
                try
                {
                    mPreview.Start(mCameraSource, mGraphicOverlay);
                }
                catch (System.Exception e)
                {
                    Log.Error(TAG, "Unable to start camera source.", e);
                    mCameraSource.Release();
                    mCameraSource = null;
                }
            }
        }
        public Tracker Create(Java.Lang.Object item)
        {
            return new GraphicFaceTracker(mGraphicOverlay, mCameraSource);
        }

        //Если не доступна камера
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
        {
            if (requestCode != RC_HANDLE_CAMERA_PERM)
            {
                Log.Debug(TAG, "Got unexpected permission result: " + requestCode);
                base.OnRequestPermissionsResult(requestCode, permissions, grantResults);

                return;
            }

            if (grantResults.Length != 0 && grantResults[0] == Permission.Granted)
            {
                Log.Debug(TAG, "Camera permission granted - initialize the camera source");
                // we have permission, so create the camerasource
                CreateCameraSource();
                return;
            }

            Log.Error(TAG, "Permission not granted: results len = " + grantResults.Length +
                    " Result code = " + (grantResults.Length > 0 ? grantResults[0].ToString() : "(empty)"));


            var builder = new Android.Support.V7.App.AlertDialog.Builder(this);
            builder.SetTitle("LiveCam")
                    .SetMessage(Resource.String.no_camera_permission)
                    .SetPositiveButton(Resource.String.ok, (o, e) => Finish())
                    .Show();

        }
    }


    class GraphicFaceTracker : Tracker, CameraSource.IPictureCallback
    {
        private GraphicOverlay mOverlay;
        private FaceGraphic mFaceGraphic;
        private CameraSource mCameraSource = null;
        private bool isProcessing = false;
        public static int response_id = 0;

        public GraphicFaceTracker(GraphicOverlay overlay, CameraSource cameraSource = null)
        {
            mOverlay = overlay;
            mFaceGraphic = new FaceGraphic(overlay);
            mCameraSource = cameraSource;
        }
        //Обнаружение нового лица
        public override void OnNewItem(int id, Java.Lang.Object item)
        {
            mFaceGraphic.SetId(id);
            if (mCameraSource != null && !isProcessing)
            {
                if (MainActivity.facesList != null && MainActivity.data != null)
                {
                    if (MainActivity.facesList.Count != MainActivity.data.Count)
                    {
                        MainActivity.facesList.Clear();// = null;
                        MainActivity.data.Clear(); //= null;
                    }
                }
                try
                {
                    mCameraSource.TakePicture(null, this);
                }
                catch (RuntimeException)
                {

                    System.Console.WriteLine("TakePicture failed!");
                }
                //mCameraSource.TakePicture(null, this);
            }
            //System.Console.WriteLine("Id лица" + MainActivity.facesList[0].ToString());
        }
        public override void OnUpdate(Detector.Detections detections, Java.Lang.Object item)
        {
            var face = item as Face;
            mOverlay.Add(mFaceGraphic);
            mFaceGraphic.UpdateFace(face);
        }
        //Удаление рамки при потере лица
        public override void OnMissing(Detector.Detections detections)
        {
            System.Console.WriteLine("Missing face");
            mOverlay.Remove(mFaceGraphic);
            //MainActivity.facesList.Clear();
            MainActivity.facesList.Clear();// = null;
            MainActivity.data.Clear();// = null;            
            MainActivity.faceid_id.Clear();// = null;
            FaceGraphic.drawable = null;

        }

        public override void OnDone()
        {
            mOverlay.Remove(mFaceGraphic);

        }

        //формат передачи id
        public string ListToString(List<int> collection)
        {
            string outputString = "";
            foreach (var item in collection)
            {
                if (collection.Count >= 2)
                {
                    outputString += item.ToString() + ", ";
                }
                else
                {
                    outputString += item.ToString();
                }

            }
            return outputString;
        }

        //Отправка фотографии на сервер и получение JSON'a
        public Dictionary<int, Dictionary<string, string>> parseString(string json)
        {
            Dictionary<int, Dictionary<string, string>> output = new Dictionary<int, Dictionary<string, string>>();
            var persons = json.Split(';');
            //System.Console.WriteLine("a length = " + a.Length);
            for (int i = 0; i < persons.Length; i++)
            {
                if (persons[i] == "")
                    continue;

                var column = persons[i].Split('|');
                //Добавляем поля
                var person = new Dictionary<string, string>();
                person.Add("data", column[0]);
                person.Add("name", column[1]);
                person.Add("x", column[2]);
                person.Add("y", column[3]);
                person.Add("width", column[4]);
                person.Add("height", column[5]);
                //Добавляем в основной словарь
                output.Add(i, person);
            }
            return output;
        }

        public string sayAllNames(Dictionary<int, Dictionary<string, string>> persons)
        {
            string names = "";
            for (int i = 0; i < persons.Count; i++)
            {
                names += persons[i]["name"];
                if (i != persons.Count - 1)
                {
                    names += ", ";
                }
            }
            return names;
        }

        public static bool checkId(List<int> idList, int serchedId)
        {
            foreach (var item in idList)
            {
                System.Console.WriteLine("item = " + item);
                if (item == serchedId)
                {
                    System.Console.WriteLine("checkId = true");
                    return true;
                }
            }
            System.Console.WriteLine("checkId = false");
            return false;
        }

        public void OnPictureTaken(byte[] data)
        {
            //System.Console.WriteLine("ListFaces = " + ListToString(MainActivity.facesList));
            Task.Run(async () =>
            {
                if (data != null)
                {
                    //Stream juststream = ContentResolver.OpenInputStream();
                    MemoryStream stream = new MemoryStream();
                    Bitmap bitmap = BitmapFactory.DecodeByteArray(data, 0, data.Length);

                    bitmap.Compress(Bitmap.CompressFormat.Jpeg, 100, stream);

                    var bitmapData = Convert.ToBase64String(data);
                    var fileContent = new StringContent(bitmapData);

                    MultipartFormDataContent dataContent = new MultipartFormDataContent();
                    //dataContent.Add(fileContent, "File");
                    dataContent.Add(fileContent, "File");

                    using (var client = new HttpClient())
                    {
                        //Заголовки
                        dataContent.Headers.Add("width", (MainActivity.width / 2).ToString());
                        dataContent.Headers.Add("height", (MainActivity.height / 2).ToString());
                        //Глубина цвета
                        dataContent.Headers.Add("color", (data.Length / ((MainActivity.width / 2) * (MainActivity.height / 2))).ToString());

                        if (MainActivity.currentAppMode == MainActivity.AppMode.Faces)
                        {
                            //Faces
                            try
                            {
                                var response = await client.PostAsync(new Uri("http://192.168.2.17:9990"), dataContent);//content);
                                ++response_id;

                                if (response.IsSuccessStatusCode)
                                {
                                    string recievedContent = await response.Content.ReadAsStringAsync();

                                    System.Console.WriteLine("content = " + recievedContent);

                                    MainActivity.data = parseString(recievedContent);
                                    //Читаем все имена, распознанные на фото
                                    MainActivity.textToSpeech.Speak(sayAllNames(MainActivity.data), QueueMode.Flush, null);
                                }
                                else
                                {
                                    System.Console.WriteLine("Подключение не удалось!");
                                }
                            }

                            catch (HttpRequestException)
                            {
                                System.Console.WriteLine("Подключение не удалось! HttpRequetsExeption");
                            }
                        }

                        else if (MainActivity.currentAppMode == MainActivity.AppMode.Things)
                        {
                            //Things
                            //try
                            //{
                            //    var response = await client.PostAsync(new Uri("http://192.168.2.17:9990"), dataContent);//content);
                            //    ++response_id;

                            //    if (response.IsSuccessStatusCode)
                            //    {
                            //        string recievedContent = await response.Content.ReadAsStringAsync();

                            //        System.Console.WriteLine("content = " + recievedContent);

                            //        MainActivity.data = parseString(recievedContent);
                            //        //Читаем все имена, распознанные на фото
                            //        MainActivity.textToSpeech.Speak(sayAllNames(MainActivity.data), QueueMode.Flush, null);
                            //    }
                            //    else
                            //    {
                            //        System.Console.WriteLine("Подключение не удалось!");
                            //    }
                            //}

                            //catch (HttpRequestException)
                            //{
                            //    System.Console.WriteLine("Подключение не удалось! HttpRequetsExeption");
                            //}
                        }
                    }


                }
            });

            Task.Run(async () =>
            {
                try
                {
                    isProcessing = true;
                }

                finally
                {
                    isProcessing = false;
                }
            });
        }
    }
}


