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
        //static Dictionary<string, string> ParseJson(string res)
        //{
        //    var lines = res.Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
        //    var ht = new Dictionary<string, string>(20);
        //    var st = new Stack<string>(20);

        //    for (int i = 0; i < lines.Length; ++i)
        //    {
        //        var line = lines[i];
        //        var pair = line.Split(":".ToCharArray(), 2, StringSplitOptions.RemoveEmptyEntries);

        //        if (pair.Length == 2)
        //        {
        //            var key = ClearString(pair[0]);
        //            var val = ClearString(pair[1]);

        //            if (val == "{")
        //            {
        //                st.Push(key);
        //            }
        //            else
        //            {
        //                if (st.Count > 0)
        //                {
        //                    key = string.Join("_", st) + "_" + key;
        //                }

        //                if (ht.ContainsKey(key))
        //                {
        //                    ht[key] += "&" + val;
        //                }
        //                else
        //                {
        //                    ht.Add(key, val);
        //                }
        //            }
        //        }
        //        else if (line.IndexOf('}') != -1 && st.Count > 0)
        //        {
        //            st.Pop();
        //        }
        //    }

        //    return ht;
        //}

        //static string ClearString(string str)
        //{
        //    str = str.Trim();

        //    var ind0 = str.IndexOf("\"");
        //    var ind1 = str.LastIndexOf("\"");

        //    if (ind0 != -1 && ind1 != -1)
        //    {
        //        str = str.Substring(ind0 + 1, ind1 - ind0 - 1);
        //    }
        //    else if (str[str.Length - 1] == ',')
        //    {
        //        str = str.Substring(0, str.Length - 1);
        //    }

        //    str = HttpUtility.UrlDecode(str);

        //    return str;
        //}
        //Отправка фотографии на сервер и получение JSON'a
        public Dictionary<int, Dictionary<string, string>> parseString(string str)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            Dictionary<int, Dictionary<string, string>> output = new Dictionary<int, Dictionary<string, string>>();
            var a = str.Split(';');
            //System.Console.WriteLine("a length = " + a.Length);
            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != "")
                {
                    var b = a[i].Split('|');
                    result.Add("data", b[0]);
                    result.Add("name", b[1]);
                    result.Add("x", b[2]);
                    result.Add("y", b[3]);
                    result.Add("width", b[4]);
                    result.Add("height", b[5]);
                    output.Add(i, result);
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
                                parseString("2018-02-22 17:40:11|Unknown|54|102|88|88;");

                                //System.Console.WriteLine("NewStr[0]" + newstr[0]);


                                //MainActivity.photoName = recievedContent;
                                //JObject jObject = JObject.Parse(recievedContent);
                                //Получаем объект

                                //MainActivity.recievedJson = JsonConvert.DeserializeObject<RecievedJson>(recievedContent);
                                //string Name = MainActivity.recievedJson.name;
                                //dictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(recievedContent);
                                ////Не работает
                                //System.Console.WriteLine("JSON = " + MainActivity.recievedJson.roi.ToString() + ", " + MainActivity.recievedJson.name.ToString());

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


