using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.Media;
using Android.OS;
using Android.Support.V4.App;
using System;
using System.Globalization;
using System.Threading.Tasks;
using Xamarin.Essentials;

#if DEBUG
[assembly: Application (Debuggable = true)]
#else
[assembly: Application (Debuggable = false)]
#endif

namespace RD_AAOW.Droid
	{
	/// <summary>
	/// Класс описывает загрузчик приложения
	/// </summary>
	[Activity (Label = "UniNotifier", Icon = "@mipmap/icon", Theme = "@style/MainTheme",
		ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation,
		ScreenOrientation = ScreenOrientation.Landscape)]
	public class MainActivity: global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
		{
		/// <summary>
		/// Обработчик события создания экземпляра
		/// </summary>
		protected override void OnCreate (Bundle savedInstanceState)
			{
			// Базовая настройка
			TabLayoutResource = Resource.Layout.Tabbar;
			ToolbarResource = Resource.Layout.Toolbar;

			base.OnCreate (savedInstanceState);
			global::Xamarin.Forms.Forms.Init (this, savedInstanceState);

			// Остановка службы для настройки
			Intent mainService = new Intent (this, typeof (MainService));
			StopService (mainService);

			// Выбор требуемого экрана для отображения
			uint currentTab = 1;    // Страница настроек (по умолчанию)
			var tab = Intent.GetSerializableExtra ("Tab");
			if (tab != null)
				{
				try
					{
					currentTab = uint.Parse (tab.ToString ());
					}
				catch { }
				}

			// Запуск
			LoadApplication (new App (currentTab, Build.VERSION.SdkInt < BuildVersionCodes.Q));
			}

		/// <summary>
		/// Перезапуск службы
		/// </summary>
		protected override void OnStop ()
			{
			Intent mainService = new Intent (this, typeof (MainService));

			if (NotificationsSupport.AllowServiceToStart)
				{
				if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
					StartForegroundService (mainService);
				else
					StartService (mainService);
				}

			// Добавим уверенности в том, что служба не запустится, если она отключена
			else
				{
				StopService (mainService);
				}

			base.OnStop ();
			}
		}

	/// <summary>
	/// Класс описывает экран-заставку приложения
	/// </summary>
	[Activity (Theme = "@style/SplashTheme", MainLauncher = true, NoHistory = true,
		ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
	public class SplashActivity: global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
		{
		/// <summary>
		/// Обработчик события создания экземпляра
		/// </summary>
		protected override void OnCreate (Bundle savedInstanceState)
			{
			base.OnCreate (savedInstanceState);
			}

		/// <summary>
		/// Отключение кнопки возврата
		/// </summary>
		public override void OnBackPressed ()
			{
			}

		/// <summary>
		/// Запуск основного действия после завершения загрузки
		/// </summary>
		protected override void OnResume ()
			{
			base.OnResume ();
			Task startup = new Task (() =>
				{
					StartActivity (new Intent (Application.Context, typeof (MainActivity)));
				});
			startup.Start ();
			}
		}

	/// <summary>
	/// Класс описывает фоновую службу новостей приложения
	/// </summary>
	[Service (Name = "com.RD_AAOW.UniNotifier", Label = "UniNotifier", Icon = "@mipmap/icon", Exported = true)]
	public class MainService: global::Android.App.Service
		{
		// Основной набор оповещений
		private NotificationsSet ns;
		private SupportedLanguages al = Localization.CurrentLanguage;
		private DateTime lastNotStamp = new DateTime (2000, 1, 1, 0, 0, 0);

		// Идентификаторы процесса
		private Handler handler;
		private Action runnable;
		private bool isStarted = false;

		// Дескрипторы уведомлений
		private NotificationCompat.Builder notBuilder;
		private NotificationManager notManager;
		private const int notServiceID = 4415;
		private NotificationCompat.BigTextStyle notTextStyle;

		private Intent masterIntent;
		private Intent[] actIntent = new Intent[3];
		private PendingIntent masterPendingIntent;
		private PendingIntent[] actPendingIntent = new PendingIntent[3];

		/// <summary>
		/// Обработчик события создания службы
		/// </summary>
		public override void OnCreate ()
			{
			// Базовая обработка
			base.OnCreate ();

			// Запуск в бэкграунде
			handler = new Handler ();

			// Аналог таймера (создаёт задание, которое само себя ставит в очередь исполнения ОС)
			runnable = new Action (() =>
				{
					if (isStarted)
						{
						TimerTick ();
						handler.PostDelayed (runnable, ProgramDescription.MasterTimerDelay);
						}
				});
			}

		// Основной метод службы
		private void TimerTick ()
			{
			// Выполнение сброса при необходимости
			if (NotificationsSupport.ResetRequested)
				{
				ns.ResetTimer (!NotificationsSupport.NotCompleteReset);
				NotificationsSupport.ResetRequested = false;
				}

			// Запрос следующего сообщения
			string newText;
			if ((newText = ns.GetNextNotification ()) == "")
				return;

			// Сборка сообщения
			notBuilder.SetContentText (newText);
			notTextStyle.BigText (newText);

			if (ns.CurrentNotificationNumber < ns.Notifications.Count)
				NotificationsSupport.CurrentLink = ns.Notifications[ns.CurrentNotificationNumber].Link;
			else
				NotificationsSupport.CurrentLink = ns.SpecialNotifications[ns.CurrentSpecialNotificationNumber].Link;

			if (DateTime.Today > lastNotStamp)
				{
				if (NotificationsSupport.MasterLog != "")
					{
					// Запрос текущего формата представления даты
					CultureInfo ci;
					try
						{
						if (al == SupportedLanguages.ru_ru)
							ci = new CultureInfo ("ru-ru");
						else
							ci = new CultureInfo ("en-us");
						}
					catch
						{
						ci = CultureInfo.InstalledUICulture;
						}

					// Добавление отступа
					if (lastNotStamp.Year == 2000)
						NotificationsSupport.MasterLog = Localization.GetText ("EarlierMessage", al) +
							"\r\n\r\n" + NotificationsSupport.MasterLog;
					else
						NotificationsSupport.MasterLog = "--- " + lastNotStamp.ToString (ci.DateTimeFormat.LongDatePattern, ci) +
							" ---\r\n\r\n" + NotificationsSupport.MasterLog;
					}

				lastNotStamp = DateTime.Today;
				}
			NotificationsSupport.MasterLog = newText + "\r\n\r\n\r\n" + NotificationsSupport.MasterLog;

			// Отправка
			Android.App.Notification notification = notBuilder.Build ();
			notManager.Notify (notServiceID, notification);

			// Сброс
			notification.Dispose ();
			}

		/// <summary>
		/// Обработчик события запуска службы
		/// </summary>
		public override StartCommandResult OnStartCommand (Intent intent, StartCommandFlags flags, int startId)
			{
			// Защита
			if (isStarted)
				return StartCommandResult.Sticky;

			// Инициализация объектов настройки
			notManager = (NotificationManager)GetSystemService (NotificationService);
			notBuilder = new NotificationCompat.Builder (this, ProgramDescription.AssemblyMainName.ToLower ());

			// Создание канала (для Android O и выше)
			if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
				{
				NotificationChannel channel = new NotificationChannel (ProgramDescription.AssemblyMainName.ToLower (),
					ProgramDescription.AssemblyMainName, NotificationImportance.High);

				// Настройка
				channel.Description = ProgramDescription.AssemblyTitle;
				if (NotificationsSupport.AllowLight)
					{
					channel.LightColor = Color.Green;
					channel.EnableLights (true);
					}
				if (NotificationsSupport.AllowVibro)
					{
					channel.SetVibrationPattern (new long[] { 200, 600, 200 });
					channel.EnableVibration (true);
					}
				if (NotificationsSupport.AllowSound)
					{
					AudioAttributes att = new AudioAttributes.Builder ()
						.SetUsage (AudioUsageKind.Notification)
						.SetContentType (AudioContentType.Music)
						.Build ();
					channel.SetSound (RingtoneManager.GetDefaultUri (RingtoneType.Notification), att);
					}
				channel.LockscreenVisibility = NotificationsSupport.AllowOnLockedScreen ? NotificationVisibility.Public :
					NotificationVisibility.Private;

				// Запуск
				notManager.CreateNotificationChannel (channel);
				notBuilder.SetChannelId (ProgramDescription.AssemblyMainName.ToLower ());
				}

			// Инициализация сообщений
			notBuilder.SetCategory ("CategoryMessage");
			notBuilder.SetColor (0x80FFC0);     // Оттенок заголовков оповещений

			string launchMessage = Localization.GetText ("LaunchMessage", al) +
				((Build.VERSION.SdkInt >= BuildVersionCodes.Q) ? Localization.GetText ("LaunchMessage10", al) : "");
			notBuilder.SetContentText (launchMessage);
			notBuilder.SetContentTitle (ProgramDescription.AssemblyTitle);

			if (Build.VERSION.SdkInt < BuildVersionCodes.O)
				{
				notBuilder.SetDefaults (0);         // Для служебного сообщения
				notBuilder.SetPriority ((int)NotificationPriority.Default);
				}
			else
				{
				notBuilder.SetDefaults ((int)(NotificationsSupport.AllowSound ? NotificationDefaults.Sound : 0) |
					(int)(NotificationsSupport.AllowLight ? NotificationDefaults.Lights : 0) |
					(int)(NotificationsSupport.AllowVibro ? NotificationDefaults.Vibrate : 0));
				notBuilder.SetPriority ((int)NotificationPriority.High);

				notBuilder.SetLights (0x00FF80, 1000, 1000);
				notBuilder.SetVibrate (new long[] { 200, 600, 200 });

				if (NotificationsSupport.AllowSound)
					notBuilder.SetSound (RingtoneManager.GetDefaultUri (RingtoneType.Notification));
				}

			notBuilder.SetSmallIcon (Resource.Drawable.ic_not);
			if ((Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop) &&
				((DeviceInfo.Idiom == DeviceIdiom.Desktop) || (DeviceInfo.Idiom == DeviceIdiom.Tablet) ||
				(DeviceInfo.Idiom == DeviceIdiom.TV) || (Build.VERSION.SdkInt < BuildVersionCodes.N)))
				{
				notBuilder.SetLargeIcon (BitmapFactory.DecodeResource (this.Resources, Resource.Drawable.ic_not_large));
				}

			notBuilder.SetVisibility (NotificationsSupport.AllowOnLockedScreen ? (int)NotificationVisibility.Public :
				(int)NotificationVisibility.Private);

			notTextStyle = new NotificationCompat.BigTextStyle (notBuilder);
			notTextStyle.BigText (launchMessage);

			// Основные действия управления
			for (int i = 0; i < actIntent.Length; i++)
				{
				if (i != 1)
					{
					actIntent[i] = new Intent (this, typeof (MainActivity));
					actIntent[i].PutExtra ("Tab", i / 2);
					actPendingIntent[i] = PendingIntent.GetActivity (this, i, actIntent[i], 0);
					}
				else
					{
					actIntent[i] = new Intent (this, typeof (NotificationReset));
					actPendingIntent[i] = PendingIntent.GetService (this, i, actIntent[i], 0);
					}
				notBuilder.AddAction (new NotificationCompat.Action.Builder (0,
					Localization.GetText ("CommandButton" + i.ToString (), al), actPendingIntent[i]).Build ());
				}

			// Стартовое сообщение
			Android.App.Notification notification = notBuilder.Build ();
			StartForeground (notServiceID, notification);

			// Перенастройка для основного режима
			if (Build.VERSION.SdkInt < BuildVersionCodes.O)
				{
				notBuilder.SetDefaults ((int)(NotificationsSupport.AllowSound ? NotificationDefaults.Sound : 0) |
					(int)(NotificationsSupport.AllowLight ? NotificationDefaults.Lights : 0) |
					(int)(NotificationsSupport.AllowVibro ? NotificationDefaults.Vibrate : 0));
				notBuilder.SetPriority ((int)NotificationPriority.Max);
				}
			else
				{
				}

			masterIntent = new Intent (this, typeof (NotificationLink));
			masterPendingIntent = PendingIntent.GetService (this, 10, masterIntent, 0);
			notBuilder.SetContentIntent (masterPendingIntent);

			// Инициализация оповещений и запроса шаблонов
			ns = new NotificationsSet (true);

			// Запуск петли
			handler.PostDelayed (runnable, ProgramDescription.MasterTimerDelay);
			isStarted = true;

			return StartCommandResult.Sticky;
			}

		/// <summary>
		/// Обработчик остановки службы
		/// </summary>
		public override void OnDestroy ()
			{
			// Остановка службы
			handler.RemoveCallbacks (runnable);
			notManager.Cancel (notServiceID);
			if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
				notManager.DeleteNotificationChannel (ProgramDescription.AssemblyMainName.ToLower ());
			isStarted = false;

			// Освобождение ресурсов
			ns.Dispose ();
			notBuilder.Dispose ();
			notManager.Dispose ();

			masterIntent.Dispose ();
			masterPendingIntent.Dispose ();
			foreach (Intent i in actIntent)
				i.Dispose ();
			foreach (PendingIntent pi in actPendingIntent)
				pi.Dispose ();

			// Стандартная обработка
			base.OnDestroy ();
			}

		/// <summary>
		/// Обработчик привязки службы (заглушка)
		/// </summary>
		public override IBinder OnBind (Intent intent)
			{
			// Return null because this is a pure started service
			return null;
			}

		/// <summary>
		/// Обработчик события снятия задачи (заглушка)
		/// </summary>
		public override void OnTaskRemoved (Intent rootIntent)
			{
			//base.OnTaskRemoved(rootIntent);
			}
		}

	/// <summary>
	/// Класс описывает задание на открытие веб-ссылки
	/// </summary>
	[Service (Name = "com.RD_AAOW.UniNotifierLink", Label = "UniNotifierLink", Icon = "@mipmap/icon")]
	public class NotificationLink: IntentService
		{
		/// <summary>
		/// Конструктор (заглушка)
		/// </summary>
		public NotificationLink () : base ("NotificationLink")
			{
			}

		/// <summary>
		/// Обработка события выполнения задания
		/// </summary>
		protected override void OnHandleIntent (Intent intent)
			{
			if (NotificationsSupport.CurrentLink != "")
				Launcher.OpenAsync (NotificationsSupport.CurrentLink);
			}
		}

	/// <summary>
	/// Класс описывает задание на сброс состояния оповещений
	/// </summary>
	[Service (Name = "com.RD_AAOW.UniNotifierReset", Label = "UniNotifierReset", Icon = "@mipmap/icon")]
	public class NotificationReset: IntentService
		{
		/// <summary>
		/// Конструктор (заглушка)
		/// </summary>
		public NotificationReset () : base ("NotificationReset")
			{
			}

		/// <summary>
		/// Обработка события выполнения задания
		/// </summary>
		protected override void OnHandleIntent (Intent intent)
			{
			NotificationsSupport.ResetRequested = true;
			}
		}

	/// <summary>
	/// Класс описывает приёмник события окончания загрузки ОС
	/// </summary>
	[BroadcastReceiver (Name = "com.RD_AAOW.UniNotifierBoot", Label = "UniNotifierBoot", Icon = "@mipmap/icon")]
	public class BootReceiver: BroadcastReceiver
		{
		/// <summary>
		/// Обработчик события наступления события окончания загрузки
		/// </summary>
		public override void OnReceive (Context context, Intent intent)
			{
			if (!NotificationsSupport.AllowServiceToStart || (intent == null))
				return;

			if (intent.Action.Equals (Intent.ActionBootCompleted, StringComparison.CurrentCultureIgnoreCase) ||
				intent.Action.Equals (Intent.ActionReboot, StringComparison.CurrentCultureIgnoreCase))
				{
				Intent mainService = new Intent (context, typeof (MainService));
				if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
					context.StartForegroundService (mainService);
				else
					context.StartService (mainService);
				}
			}
		}
	}
