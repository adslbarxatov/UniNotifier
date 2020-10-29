using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
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
	public class MainActivity:global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
		{
		// Константы
		//private const int jobID = 4415;

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
			/*JobScheduler jsch = (JobScheduler)this.GetSystemService (Context.JobSchedulerService);
			jsch.Cancel (jobID);*/

			// Окно настроек
			uint currentTab = 1;    // Страница настроек (по умолчанию)
			if (Intent.GetSerializableExtra ("Tab") != null)
				{
				try
					{
					currentTab = uint.Parse (Intent.GetSerializableExtra ("Tab").ToString ());
					}
				catch { }
				}
			LoadApplication (new App (currentTab));
			}

		/// <summary>
		/// Перезапуск службы
		/// </summary>
		protected override void OnStop ()
			{
			if (NotificationsSupport.AllowServiceToStart)
				{
				Intent mainService = new Intent (this, typeof (MainService));
				if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
					StartForegroundService (mainService);
				else
					StartService (mainService);
				/*ComponentName js = new ComponentName (this, Java.Lang.Class.FromType (typeof (MainService)));
				JobInfo.Builder ji = new JobInfo.Builder (jobID, js);

				ji.SetRequiresCharging (false);
				ji.SetRequiresDeviceIdle (false);
				ji.SetRequiredNetworkType (NetworkType.Any);
				ji.SetPeriodic (15 * 60000);
				ji.SetPersisted (true);

				JobScheduler jsch = (JobScheduler)this.GetSystemService (Context.JobSchedulerService);
				jsch.Schedule (ji.Build ());	// Результат пока не важен
				*/
				}

			base.OnStop ();
			}
		}

	/// <summary>
	/// Класс описывает экран-заставку приложения
	/// </summary>
	[Activity (Theme = "@style/SplashTheme", MainLauncher = true, NoHistory = true,
		ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation,
		ScreenOrientation = ScreenOrientation.Landscape)]
	public class SplashActivity:global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
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
	public class MainService:global::Android.App.Service
		{
		// Основной набор оповещений
		private NotificationsSet ns;
		private SupportedLanguages al = Localization.CurrentLanguage;
		private DateTime lastNotStamp = new DateTime (2000, 1, 1, 0, 0, 0);

		private const long masterDelay = 10000;

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
		private Intent[] actIntent = new Intent[2];
		private PendingIntent masterPendingIntent;
		private PendingIntent[] actPendingIntent = new PendingIntent[2];

		/*
		/// <summary>
		/// Обработчик события создания задания
		/// </summary>
		public override bool OnStartJob (JobParameters jobParams)
			{
			if (!isStarted)
				{
				// Инициализация оповещений
				ns = new NotificationsSet ();

				// Инициализация сообщений
				notBuilder = new NotificationCompat.Builder (this, ProgramDescription.AssemblyMainName);
				notBuilder.SetCategory ("CategoryMessage");
				notBuilder.SetColor (0x80FFC0);     // Оттенок заголовков оповещений
				notBuilder.SetContentText (Localization.GetText ("LaunchMessage", al));
				notBuilder.SetContentTitle (ProgramDescription.AssemblyTitle);
				notBuilder.SetDefaults (0);         // Для служебного сообщения
				notBuilder.SetPriority ((int)NotificationPriority.Default);
				notBuilder.SetSmallIcon (Resource.Drawable.ic_not);
				notBuilder.SetVisibility ((int)NotificationVisibility.Private);

				notManager = (NotificationManager)this.GetSystemService (Context.NotificationService);
				notTextStyle = new NotificationCompat.BigTextStyle (notBuilder);

				// Стартовое сообщение
				Android.App.Notification notification = notBuilder.Build ();
				//StartForeground (notServiceID, notification);
				notManager.Notify (notServiceID, notification);

				// Перенастройка для основного режима
				notBuilder.SetDefaults ((int)(NotificationsSupport.AllowSound ? NotificationDefaults.Sound : 0) |
					(int)(NotificationsSupport.AllowLight ? NotificationDefaults.Lights : 0) |
					(int)(NotificationsSupport.AllowVibro ? NotificationDefaults.Vibrate : 0));
				notBuilder.SetPriority ((int)NotificationPriority.Max);

				masterIntent = new Intent (this, typeof (NotificationLink));
				masterPendingIntent = PendingIntent.GetService (this, 0, masterIntent, 0);
				notBuilder.SetContentIntent (masterPendingIntent);

				isStarted = true;
				}

			// Выполнение заданий
			for (int i = 0; i < NotificationsSet.MaxNotifications; i++)
				{
				Task.Run (() =>
				{
					TimerTick ();
				});

				// Пауза в этом потоке
				Thread.Sleep (45000);
				}

			// Отметка о завершении задания
			JobFinished (jobParams, false);

			// Завершено
			return false;
			}

		/// <summary>
		/// Обработчик события остановки задания
		/// </summary>
		public override bool OnStopJob (JobParameters jobParams)
			{
			// Освобождение ресурсов
			ns.Dispose ();
			notBuilder.Dispose ();
			notManager.Dispose ();

			masterIntent.Dispose ();
			masterPendingIntent.Dispose ();

			isStarted = false;

			// При остановке или прерывании повторное создание задания не предусмотрено
			return false;
			}
		*/

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
						handler.PostDelayed (runnable, masterDelay);
						}
				});
			}

		// Основной метод службы
		private void TimerTick ()
			{
			string newText;
			if ((newText = ns.GetNextNotification ()) == "")
				return;

			// Сборка сообщения
			notBuilder.SetContentText (newText);
			notTextStyle.BigText (newText);

			NotificationsSupport.CurrentLink = ns.Notifications[ns.CurrentNotificationNumber].Link;
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
			if (!isStarted)
				{
				// Инициализация оповещений
				ns = new NotificationsSet ();

				// Создание канала (для Android O и выше)
				notManager = (NotificationManager)GetSystemService (NotificationService);

				if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
					{
					Java.Lang.String channelNameJava = new Java.Lang.String (ProgramDescription.AssemblyMainName);
					NotificationChannel channel = new NotificationChannel (ProgramDescription.AssemblyMainName.ToLower (),
						channelNameJava, NotificationImportance.Max);
					channel.Description = ProgramDescription.AssemblyTitle;
					notManager.CreateNotificationChannel (channel);
					}

				// Инициализация сообщений
				notBuilder = new NotificationCompat.Builder (this, ProgramDescription.AssemblyMainName);
				notBuilder.SetCategory ("CategoryMessage");
				notBuilder.SetColor (0x80FFC0);     // Оттенок заголовков оповещений
				notBuilder.SetContentText (Localization.GetText ("LaunchMessage", al));
				notBuilder.SetContentTitle (ProgramDescription.AssemblyTitle);
				notBuilder.SetDefaults (0);         // Для служебного сообщения
				notBuilder.SetPriority ((int)NotificationPriority.Default);
				notBuilder.SetSmallIcon (Resource.Drawable.ic_not);

				if ((Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop) &&
					((DeviceInfo.Idiom == DeviceIdiom.Desktop) || (DeviceInfo.Idiom == DeviceIdiom.Tablet) ||
					(DeviceInfo.Idiom == DeviceIdiom.TV)))
					{
					notBuilder.SetLargeIcon (BitmapFactory.DecodeResource (this.Resources, Resource.Drawable.ic_not_large));
					}

				notBuilder.SetVisibility (NotificationsSupport.AllowOnLockedScreen ? (int)NotificationVisibility.Public :
					(int)NotificationVisibility.Private);

				notTextStyle = new NotificationCompat.BigTextStyle (notBuilder);

				// Основные действия управления
				for (int i = 0; i < actIntent.Length; i++)
					{
					actIntent[i] = new Intent (this, typeof (MainActivity));
					actIntent[i].PutExtra ("Tab", i);
					actPendingIntent[i] = PendingIntent.GetActivity (this, i, actIntent[i], 0);
					notBuilder.AddAction (new NotificationCompat.Action.Builder (0,
						Localization.GetText ("CommandButton" + i.ToString (), al), actPendingIntent[i]).Build ());
					}

				// Стартовое сообщение
				Android.App.Notification notification = notBuilder.Build ();
				StartForeground (notServiceID, notification);

				// Перенастройка для основного режима
				notBuilder.SetDefaults ((int)(NotificationsSupport.AllowSound ? NotificationDefaults.Sound : 0) |
					(int)(NotificationsSupport.AllowLight ? NotificationDefaults.Lights : 0) |
					(int)(NotificationsSupport.AllowVibro ? NotificationDefaults.Vibrate : 0));
				notBuilder.SetPriority ((int)NotificationPriority.Max);

				masterIntent = new Intent (this, typeof (NotificationLink));
				masterPendingIntent = PendingIntent.GetService (this, 10, masterIntent, 0);
				notBuilder.SetContentIntent (masterPendingIntent);

				// Запуск петли
				handler.PostDelayed (runnable, masterDelay);
				isStarted = true;
				}

			return StartCommandResult.Sticky;
			}

		/// <summary>
		/// Обработчик остановки службы
		/// </summary>
		public override void OnDestroy ()
			{
			// Остановка службы
			handler.RemoveCallbacks (runnable);
			//NotificationManager notificationManager = (NotificationManager)GetSystemService (NotificationService);
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
	/// Класс описывает запускаемое задание загрузки данных
	/// </summary>
	[Service (Name = "com.RD_AAOW.UniNotifierLink", Label = "UniNotifierLink", Icon = "@mipmap/icon")]
	public class NotificationLink:IntentService
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
	/// Класс описывает приёмник события окончания загрузки ОС
	/// </summary>
	[BroadcastReceiver (Name = "com.RD_AAOW.UniNotifierBoot", Label = "UniNotifierBoot", Icon = "@mipmap/icon")]
	public class BootReceiver:BroadcastReceiver
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
