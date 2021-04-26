using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.OS;
using Android.Support.V4.App;
using System;
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
	/// Класс описывает набор состояний сборки
	/// </summary>
	public static class UniNotifierGenerics
		{
		/// <summary>
		/// Возвращает true, если в данной версии ОС оповещения можно настроить из приложения
		/// </summary>
		public static bool AreNotificationsConfigurable
			{
			get
				{
				return (Build.VERSION.SdkInt < BuildVersionCodes.Q);
				}
			}

		/// <summary>
		/// Возвращает true, если в данной версии ОС доступен полноценный режим Foreground.
		/// Также возвращает true, если в системе доступен объект NotificationChannel
		/// </summary>
		public static bool IsForegroundAvailable
			{
			get
				{
				return (Build.VERSION.SdkInt >= BuildVersionCodes.O);
				}
			}

		/// <summary>
		/// Возвращает true, если в данной версии ОС большая иконка оповещений необходима
		/// для их корректного отображения
		/// </summary>
		public static bool IsLargeIconRequired
			{
			get
				{
				return (Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop) &&
				((DeviceInfo.Idiom == DeviceIdiom.Desktop) || (DeviceInfo.Idiom == DeviceIdiom.Tablet) ||
				(DeviceInfo.Idiom == DeviceIdiom.TV) || (Build.VERSION.SdkInt < BuildVersionCodes.N));
				}
			}
		}

	/// <summary>
	/// Класс описывает загрузчик приложения
	/// </summary>
#if TABLEPEDIA
	[Activity (Label = "Tablepedia notifier", Icon = "@mipmap/icon", Theme = "@style/MainTheme",
		ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation, Exported = true,
		ScreenOrientation = ScreenOrientation.Landscape)]
#else
	[Activity (Label = "UniNotifier", Icon = "@mipmap/icon", Theme = "@style/MainTheme",
		ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation, Exported = true,
		ScreenOrientation = ScreenOrientation.Landscape)]
#endif
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

			// Запуск независимо от разрешения
			Intent mainService = new Intent (this, typeof (MainService));
			NotificationsSupport.StopRequested = false;

			if (UniNotifierGenerics.IsForegroundAvailable)
				StartForegroundService (mainService);
			else
				StartService (mainService);

			// Выбор требуемого экрана для отображения (по умолчанию - страница настроек для UN и страница ссылок для TP)
			uint currentTab = 0;
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
			LoadApplication (new App (currentTab, UniNotifierGenerics.AreNotificationsConfigurable,
				UniNotifierGenerics.IsForegroundAvailable));
			}

		/// <summary>
		/// Перезапуск службы
		/// </summary>
		protected override void OnStop ()
			{
			Intent mainService = new Intent (this, typeof (MainService));

			// Запрос на остановку при необходимости
			if (!NotificationsSupport.AllowServiceToStart)
				NotificationsSupport.StopRequested = true;
			// Иначе служба продолжит работу в фоне

			base.OnStop ();
			}

		/// <summary>
		/// Перезапуск основного приложения
		/// </summary>
		protected override void OnResume ()
			{
			// Перезапуск, если была остановлена (независимо от разрешения)
			Intent mainService = new Intent (this, typeof (MainService));
			NotificationsSupport.StopRequested = false;

			if (UniNotifierGenerics.IsForegroundAvailable)
				StartForegroundService (mainService);
			else
				StartService (mainService);

			base.OnResume ();
			}
		}

	/// <summary>
	/// Класс описывает экран-заставку приложения
	/// </summary>
	[Activity (Theme = "@style/SplashTheme", MainLauncher = true, NoHistory = true, Exported = true,
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
#if TABLEPEDIA
	[Service (Name = "com.RD_AAOW.TablepediaNotifier", Label = "Tablepedia notifier",
		Icon = "@mipmap/icon", Exported = true)]
#else
	[Service (Name = "com.RD_AAOW.UniNotifier", Label = "UniNotifier",
		Icon = "@mipmap/icon", Exported = true)]
#endif
	public class MainService: global::Android.App.Service
		{
		// Основной набор оповещений
		private SupportedLanguages al = Localization.CurrentLanguage;

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
		private PendingIntent masterPendingIntent;

		private BroadcastReceiver[] bcReceivers = new BroadcastReceiver[2];

		private const uint timerDividerLimit = 4;

		/// <summary>
		/// Обработчик события создания службы
		/// </summary>
		public override void OnCreate ()
			{
			// Базовая обработка
			base.OnCreate ();

			// Запуск в бэкграунде
			handler = new Handler (Looper.MainLooper);

			// Аналог таймера (создаёт задание, которое само себя ставит в очередь исполнения ОС)
			runnable = new Action (() =>
				{
					if (isStarted)
						{
						TimerTick ();
						handler.PostDelayed (runnable, ProgramDescription.MasterTimerDelay / timerDividerLimit);
						}
				});
			}

		// Основной метод службы
		private void TimerTick ()
			{
			// Контроль требования завершения службы (игнорирует все прочие флаги)
			if (NotificationsSupport.StopRequested)
				{
				Intent mainService = new Intent (this, typeof (MainService));
				StopService (mainService);

				return;
				}
			}

		/// <summary>
		/// Обработчик события запуска службы
		/// </summary>
		public override StartCommandResult OnStartCommand (Intent intent, StartCommandFlags flags, int startId)
			{
			// Защита
			if (isStarted)
				return StartCommandResult.NotSticky;

			// Инициализация объектов настройки
			notManager = (NotificationManager)this.GetSystemService (Service.NotificationService);
			notBuilder = new NotificationCompat.Builder (this, ProgramDescription.AssemblyMainName.ToLower ());

			// Создание канала (для Android O и выше)
			if (UniNotifierGenerics.IsForegroundAvailable)
				{
				NotificationChannel channel = new NotificationChannel (ProgramDescription.AssemblyMainName.ToLower (),
					ProgramDescription.AssemblyMainName, NotificationImportance.High);

				// Настройка
				channel.Description = ProgramDescription.AssemblyTitle;
				/*if (NotificationsSupport.AllowLight)
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
					}*/
				channel.LockscreenVisibility = /*NotificationsSupport.AllowOnLockedScreen ?*/ NotificationVisibility.Public /*:
					NotificationVisibility.Private*/;

				// Запуск
				notManager.CreateNotificationChannel (channel);
				notBuilder.SetChannelId (ProgramDescription.AssemblyMainName.ToLower ());
				}

			// Инициализация сообщений
			notBuilder.SetCategory ("msg");     // Категория "сообщение"
			notBuilder.SetColor (0x80FFC0);     // Оттенок заголовков оповещений

			string launchMessage = Localization.GetText ("LaunchMessage", al) +
				(UniNotifierGenerics.AreNotificationsConfigurable ? "" : Localization.GetText ("LaunchMessage10", al));
			notBuilder.SetContentText (launchMessage);
			notBuilder.SetContentTitle (ProgramDescription.AssemblyTitle);
			notBuilder.SetTicker (ProgramDescription.AssemblyTitle);

			// Настройка видимости для стартового сообщения
			if (!UniNotifierGenerics.IsForegroundAvailable)
				{
				notBuilder.SetDefaults (0);         // Для служебного сообщения
				notBuilder.SetPriority ((int)NotificationPriority.Default);
				}
			else
				{
				notBuilder.SetDefaults (0 /*(int)(NotificationsSupport.AllowSound ? NotificationDefaults.Sound : 0) |
					(int)(NotificationsSupport.AllowLight ? NotificationDefaults.Lights : 0) |
					(int)(NotificationsSupport.AllowVibro ? NotificationDefaults.Vibrate : 0)*/);
				notBuilder.SetPriority ((int)NotificationPriority.High);

				notBuilder.SetLights (0x00FF80, 1000, 1000);
				notBuilder.SetVibrate (new long[] { 200, 600, 200 });

				/*if (NotificationsSupport.AllowSound)
					notBuilder.SetSound (RingtoneManager.GetDefaultUri (RingtoneType.Notification));*/
				}

			notBuilder.SetSmallIcon (Resource.Drawable.ic_not);
			if (UniNotifierGenerics.IsLargeIconRequired)
				{
				notBuilder.SetLargeIcon (BitmapFactory.DecodeResource (this.Resources, Resource.Drawable.ic_not_large));
				}

			notBuilder.SetVisibility (/*NotificationsSupport.AllowOnLockedScreen ?*/ (int)NotificationVisibility.Public /*:
				(int)NotificationVisibility.Private*/);

			notTextStyle = new NotificationCompat.BigTextStyle (notBuilder);
			notTextStyle.BigText (launchMessage);

			// Основные действия управления
			/*for (int i = 0; i < actIntent.Length; i++)
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
				}*/

			// Прикрепление ссылки для перехода в основное приложение
			masterIntent = new Intent (this, typeof (NotificationLink));
			masterPendingIntent = PendingIntent.GetService (this, 0, masterIntent, 0);
			notBuilder.SetContentIntent (masterPendingIntent);

			// Стартовое сообщение
			Android.App.Notification notification = notBuilder.Build ();
			StartForeground (notServiceID, notification);

			// Перенастройка для основного режима
			if (!UniNotifierGenerics.IsForegroundAvailable)
				{
				notBuilder.SetDefaults (0 /*(int)(NotificationsSupport.AllowSound ? NotificationDefaults.Sound : 0) |
					(int)(NotificationsSupport.AllowLight ? NotificationDefaults.Lights : 0) |
					(int)(NotificationsSupport.AllowVibro ? NotificationDefaults.Vibrate : 0)*/);
				notBuilder.SetPriority ((int)NotificationPriority.Max);
				}

			// Запуск петли
			this.RegisterReceiver (bcReceivers[0] = new BootReceiver (),
				new IntentFilter (Intent.ActionBootCompleted));
			this.RegisterReceiver (bcReceivers[1] = new BootReceiver (),
				new IntentFilter ("android.intent.action.QUICKBOOT_POWERON"));

			handler.PostDelayed (runnable, ProgramDescription.MasterTimerDelay / timerDividerLimit);
			isStarted = true;

			return StartCommandResult.NotSticky;
			}

		/// <summary>
		/// Обработчик остановки службы
		/// </summary>
		public override void OnDestroy ()
			{
			// Остановка службы
			handler.RemoveCallbacks (runnable);
			notManager.Cancel (notServiceID);
			if (UniNotifierGenerics.IsForegroundAvailable)
				notManager.DeleteNotificationChannel (ProgramDescription.AssemblyMainName.ToLower ());
			isStarted = false;

			// Освобождение ресурсов
			notBuilder.Dispose ();
			notManager.Dispose ();

			masterIntent.Dispose ();
			masterPendingIntent.Dispose ();

			// Глушение
			if (UniNotifierGenerics.IsForegroundAvailable)
				StopForeground (StopForegroundFlags.Remove);
			else
				StopForeground (true);
			StopSelf ();

			foreach (BroadcastReceiver br in bcReceivers)
				this.UnregisterReceiver (br);

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
			}
		}

	/// <summary>
	/// Класс описывает задание на открытие веб-ссылки
	/// </summary>
#if TABLEPEDIA
	[Service (Name = "com.RD_AAOW.TablepediaNotifierLink", Label = "TablepediaNotifierLink",
		Icon = "@mipmap/icon", Exported = true)]
#else
	[Service (Name = "com.RD_AAOW.UniNotifierLink", Label = "UniNotifierLink",
		Icon = "@mipmap/icon", Exported = true)]
#endif
	public class NotificationLink: JobIntentService
		{
		/// <summary>
		/// Конструктор (заглушка)
		/// </summary>
		public NotificationLink ()
			{
			}

		/// <summary>
		/// Обработка события выполнения задания
		/// </summary>
		protected override void OnHandleWork (Intent intent)
			{
			if (NotificationsSupport.AppIsRunning || (intent == null))
				return;

			NotificationsSupport.StopRequested = false;
			Intent mainActivity = new Intent (this, typeof (MainActivity));
			mainActivity.PutExtra ("Tab", 0);
			PendingIntent.GetActivity (this, 0, mainActivity, 0).Send ();
			}
		}

	/*	/// <summary>
		/// Класс описывает задание на сброс состояния оповещений
		/// </summary>
	#if TABLEPEDIA
		[Service (Name = "com.RD_AAOW.TablepediaNotifierReset", Label = "TablepediaNotifierReset",
			Icon = "@mipmap/icon", Exported = true)]
	#else
		[Service (Name = "com.RD_AAOW.UniNotifierReset", Label = "UniNotifierReset",
			Icon = "@mipmap/icon", Exported = true)]
	#endif
		public class NotificationReset: JobIntentService
			{
			/// <summary>
			/// Конструктор (заглушка)
			/// </summary>
			public NotificationReset ()
				{
				}

			/// <summary>
			/// Обработка события выполнения задания
			/// </summary>
			protected override void OnHandleWork (Intent intent)
				{
				NotificationsSupport.ResetRequested = true;
				}
			}*/

	/// <summary>
	/// Класс описывает приёмник события окончания загрузки ОС
	/// </summary>
#if TABLEPEDIA
	[BroadcastReceiver (Name = "com.RD_AAOW.TablepediaNotifierBoot", Label = "TablepediaNotifierBoot",
		Icon = "@mipmap/icon", Exported = true)]
#else
	[BroadcastReceiver (Name = "com.RD_AAOW.UniNotifierBoot", Label = "UniNotifierBoot",
		Icon = "@mipmap/icon", Exported = true)]
#endif
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
				NotificationsSupport.StopRequested = false;

				if (UniNotifierGenerics.IsForegroundAvailable)
					context.StartForegroundService (mainService);
				else
					context.StartService (mainService);
				}
			}
		}

	/*/// <summary>
	/// Класс описывает приёмник события входа в систему
	/// </summary>
#if TABLEPEDIA
	[BroadcastReceiver (Name = "com.RD_AAOW.TablepediaNotifierWake", Label = "TablepediaNotifierWake",
		Icon = "@mipmap/icon", Exported = true)]
#else
	[BroadcastReceiver (Name = "com.RD_AAOW.UniNotifierWake", Label = "UniNotifierWake",
		Icon = "@mipmap/icon", Exported = true)]
#endif
	public class WakeReceiver: BroadcastReceiver
		{
		/// <summary>
		/// Обработчик события наступления события входа в систему
		/// </summary>
		public override void OnReceive (Context context, Intent intent)
			{
			if ((Battery.PowerSource != BatteryPowerSource.Battery) ||
				!NotificationsSupport.RequestOnUnlock ||
				!NotificationsSupport.AllowServiceToStart || (intent == null))
				return;

			if (intent.Action.Equals (Intent.ActionUserPresent, StringComparison.CurrentCultureIgnoreCase))
				NotificationsSupport.ResetRequested = true;
			}
		}*/
	}
