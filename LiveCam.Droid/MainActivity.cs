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

namespace LiveCam.Droid
{
    [Activity(Label = "LiveCam.Droid", MainLauncher = true, Icon = "@drawable/icon", Theme = "@style/Theme.AppCompat.NoActionBar", ScreenOrientation = ScreenOrientation.FullSensor)]
    public class MainActivity : AppCompatActivity, IFactory
    {
        private static readonly string TAG = "FaceTracker";

        private CameraSource mCameraSource = null;

        private CameraSourcePreview mPreview;
        private GraphicOverlay mGraphicOverlay;
        //Массив с id лиц
        public static List<int> facesList;
        public static float height;
        public static float width;
        //Для получения данных с сервера
        public static RecievedJson recievedJson;
        public static string photoName = "";
        public static Dictionary<int, Dictionary<string, string>> data;
        public static string GreetingsText
        {
            get;
            set;
        }

        private static readonly int RC_HANDLE_GMS = 9001;
        // permission request codes need to be < 256
        private static readonly int RC_HANDLE_CAMERA_PERM = 2;

        protected async override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            if (MainActivity.recievedJson == null)
            {
                MainActivity.recievedJson = new RecievedJson();
            }
            if (MainActivity.data == null)
            {
                MainActivity.data = new Dictionary<int, Dictionary<string, string>>();
            }

            if (MainActivity.facesList == null)
            {
                MainActivity.facesList = new List<int>();
            }

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
                    .SetRequestedPreviewSize((int)height, (int)width )
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
            mOverlay.Remove(mFaceGraphic);
            //Чистим список активных id
            MainActivity.facesList.Clear();
            MainActivity.photoName = "";
            MainActivity.data.Clear();

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
            Dictionary<string, string> person = new Dictionary<string, string>();
            Dictionary<int, Dictionary<string, string>> output = new Dictionary<int, Dictionary<string, string>>();
            var persons = json.Split(';');
            //System.Console.WriteLine("a length = " + a.Length);
            for (int i = 0; i < persons.Length; i++)
            {
                if (persons[i] != "")
                {
                    var column = persons[i].Split('|');
                    //Добавляем поля
                    person.Add("data", column[0]);
                    person.Add("name", column[1]);
                    person.Add("x", column[2]);
                    person.Add("y", column[3]);
                    person.Add("width", column[4]);
                    person.Add("height", column[5]);
                    //Добавляем в основной словарь
                    output.Add(i, person);
                    //Чистим основной словарь
                    person.Clear();
                }
            }
         
            return output;
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
                    //------------
                    //Сохранение в память
                    //var sdCardPath = Android.OS.Environment.ExternalStorageDirectory.AbsolutePath;
                    //var filePath = System.IO.Path.Combine(sdCardPath, "test.jpg");
                    //var filestream = new FileStream(filePath, FileMode.Create);
                    ////bitmap.Compress(Bitmap.CompressFormat.Png, 100, stream);
                    //bitmap.Compress(Bitmap.CompressFormat.Jpeg, 100, filestream);
                    //filestream.Close();
                    //------------
                    var bitmapData = Convert.ToBase64String(data);
                    var fileContent = new StringContent(bitmapData);
                    //var fileContent = new ByteArrayContent(bitmapData);
                    //for (int i = 0; i < 4; i++)
                    //{

                    //    System.Console.WriteLine("Bitmap data = "+ bitmapData[i]);
                    //}
                    MultipartFormDataContent dataContent = new MultipartFormDataContent();
                    //dataContent.Add(fileContent, "File");
                    dataContent.Add(fileContent, "File");
                    //Dictionary<string, string> dictionary = new Dictionary<string, string>();
                    //Необходимо отправлять id лиц, для последующей привязки текста к правильной рамке
                    //Создать алгоритм определения id на холсте
                    using (var client = new HttpClient())
                    {
                        //Заголовки
                        //var content = new ByteArrayContent(data);
                        //content.Headers.Add("width", (MainActivity.width / 6).ToString());
                        //content.Headers.Add("height", (MainActivity.height / 8).ToString());
                        ////Глубина цвета
                        //content.Headers.Add("color", (data.Length / ((MainActivity.width / 6) * (MainActivity.height / 8))).ToString());

                        //------------------------------------
                        //Массив id
                        dataContent.Headers.Add("Ids", ListToString(MainActivity.facesList));
                        //------------------------------------
                        dataContent.Headers.Add("width", (MainActivity.width / 2).ToString());
                        dataContent.Headers.Add("height", (MainActivity.height / 2).ToString());
                        //Глубина цвета
                        dataContent.Headers.Add("color", (data.Length / ((MainActivity.width / 2) * (MainActivity.height / 2))).ToString());

                        try
                        {
                            var response = await client.PostAsync(new Uri("http://192.168.2.17:9990"), dataContent);//content);

                            if (response.IsSuccessStatusCode)
                            {
                                string recievedContent = await response.Content.ReadAsStringAsync();

                                System.Console.WriteLine("content = " + recievedContent);

                                MainActivity.data = parseString(recievedContent);
                                
                                //test
                                //parseString("2018-02-22 17:40:11|Unknown|54|102|88|88;");
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


                }
            });
            //var content = new MultipartFormDataContent();
            ////content.Add(new StreamContent())
            //HttpClient httpClient = new HttpClient();
            //HttpRequestMessage requestMessage = new HttpRequestMessage();
            //requestMessage.RequestUri = new Uri("http://192.168.2.95:50053");
            //requestMessage.Method = HttpMethod.Post;
            ////requestMessage.Content = ;


            //--------------------------------------------------
            Task.Run(async () =>
            {
                try
                {
                    isProcessing = true;

                    System.Console.WriteLine("data = " + data.Length);

                    //var imageAnalyzer = new ImageAnalyzer(data);
                    //Console.WriteLine("type = "+imageAnalyzer.Data.GetType().ToString() );

                    //Канал подключения
                    //Grpc.Core.Channel channel = new Grpc.Core.Channel("192.168.2.95:50053", Grpc.Core.ChannelCredentials.Insecure);

                    ////Клиент
                    //ImageService.ImageService.ImageServiceClient imageService = new ImageService.ImageService.ImageServiceClient(channel);
                    //Image image = new Image();
                    //image.Data = Google.Protobuf.ByteString.CopyFrom(data);
                    //Query query = new Query();
                    //query.Id = 1;
                    //query.Image = image;
                    //Console.WriteLine("qsize="+query.CalculateSize()); 
                    //var recievedData = imageService.make_request(query);
                    //Console.WriteLine("id: " + recievedData.Id + " status: " + recievedData.Status + " decr: " + recievedData.Description);
                    //await channel.WaitForStateChangedAsync(Grpc.Core.ChannelState.Connecting);
                    //channel.ShutdownAsync().Wait();


                    //var filename = System.IO.Path.Combine(Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryPictures).ToString(), "NewFolder");
                    //Directory.CreateDirectory(filename);


                    //using (var fileOutputStream = new Java.IO.FileOutputStream(filename))
                    //{
                    //    await fileOutputStream.WriteAsync(data);
                    //}
                    //await LiveCamHelper.ProcessCameraCapture(imageAnalyzer);


                }

                finally
                {
                    ////Канал подключения
                    //Grpc.Core.Channel channel = new Grpc.Core.Channel("192.168.2.95:50053", Grpc.Core.ChannelCredentials.Insecure);

                    ////Клиент
                    //ImageService.ImageService.ImageServiceClient imageService = new ImageService.ImageService.ImageServiceClient(channel);
                    //Image image = new Image();
                    //image.Data = Google.Protobuf.ByteString.CopyFrom(data);
                    //Query query = new Query();
                    //query.Id = 1;
                    //query.Image = image;
                    //var recievedData = imageService.make_requestAsync(query);
                    //Console.WriteLine("smth");
                    //Console.WriteLine("id: " + recievedData.GetStatus()); //+ " status: " + recievedData.Status + " decr: " + recievedData.Description);
                    //channel.ShutdownAsync().Wait();

                    isProcessing = false;
                }
            });
        }
    }
}


