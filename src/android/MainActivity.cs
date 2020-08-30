using Android.App;
using Android.Content;
using Android.Content.PM;
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
	/// Класс описывает загрузчик приложения
	/// </summary>
	[Activity (Label = "UniNotifier", Icon = "@mipmap/icon", Theme = "@style/MainTheme",
		ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
	public class MainActivity:global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
		{
		/// <summary>
		/// Обработчик события создания экземпляра
		/// </summary>
		/// <param name="savedInstanceState"></param>
		protected override void OnCreate (Bundle savedInstanceState)
			{
			TabLayoutResource = Resource.Layout.Tabbar;
			ToolbarResource = Resource.Layout.Toolbar;

			base.OnCreate (savedInstanceState);
			global::Xamarin.Forms.Forms.Init (this, savedInstanceState);

			// Остановка службы для настройки
			Intent mainService = new Intent (this, typeof (MainService));
			StopService (mainService);

			// Окно настроек
			LoadApplication (new App ());
			}

		/// <summary>
		/// Перезапуск службы
		/// </summary>
		protected override void OnStop ()
			{
			if (NotificationsSupport.AllowServiceToStart)
				{
				Intent mainService = new Intent (this, typeof (MainService));
				StartService (mainService);
				}

			base.OnStop ();
			}
		}

	/// <summary>
	/// Класс описывает экран-заставку приложения
	/// </summary>
	[Activity (Theme = "@style/SplashTheme", MainLauncher = true, NoHistory = true,
		ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
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
	/// Класс описывает службу новостей приложения
	/// </summary>
	[Service (Name = "com.RD_AAOW.UniNotifier",
		Label = "UniNotifier", Icon = "@mipmap/icon", Exported = true)]
	public class MainService:global::Android.App.Service
		{
		// Основной набор оповещений
		private NotificationsSet ns;

		private const long masterDelay = 5000;

		// Идентификаторы процесса
		private Handler handler;
		private Action runnable;
		private bool isStarted = false;

		// Дескрипторы уведомлений
		private NotificationCompat.Builder notBuilder;
		private NotificationManager notManager;
		private int notID = 0;
		private NotificationCompat.BigTextStyle notTextStyle;
		private Intent masterIntent;
		private PendingIntent masterPendingIntent;

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
			if ((newText = ns.GetNextNotification ()) != "")
				{
				// Сборка сообщения
				notBuilder.SetContentText (newText);
				notTextStyle.BigText (newText);

				NotificationsSupport.CurrentLink = ns.Notifications[ns.CurrentNotificationNumber].Link;

				if (masterPendingIntent == null)
					{
					masterIntent = new Intent (this, typeof (NotificationLink));
					masterPendingIntent = PendingIntent.GetService (this, 0, masterIntent, 0);
					notBuilder.SetContentIntent (masterPendingIntent);
					}

				// Отправка
				Android.App.Notification notification = notBuilder.Build ();
				notManager.Notify (notID, notification);

				// Сброс
				notification.Dispose ();
				}
			}

		/// <summary>
		/// Обработчик события запуска службы
		/// </summary>
		/// <returns></returns>
		public override StartCommandResult OnStartCommand (Intent intent, StartCommandFlags flags, int startId)
			{
			if (!isStarted)
				{
				// Инициализация
				ns = new NotificationsSet ();

				notBuilder = new NotificationCompat.Builder (this, ProgramDescription.AssemblyMainName);
				notBuilder.SetContentTitle (ProgramDescription.AssemblyTitle);
				notBuilder.SetSmallIcon (Resource.Drawable.ic_not);
				notBuilder.SetVisibility ((int)NotificationVisibility.Private);
				notBuilder.SetCategory ("CategoryMessage");
				notBuilder.SetDefaults (0);
				notBuilder.SetPriority ((int)NotificationPriority.Default);
				notBuilder.SetContentText (Localization.GetText ("LaunchMessage", Localization.CurrentLanguage));

				notManager = (NotificationManager)this.GetSystemService (Context.NotificationService);
				notTextStyle = new NotificationCompat.BigTextStyle (notBuilder);

				// Стартовое сообщение
				Android.App.Notification notification = notBuilder.Build ();
				notManager.Notify (notID, notification);

				// Настройки основного режима
				notBuilder.SetDefaults (NotificationsSupport.AllowSound ? (int)NotificationDefaults.Sound : 0);
				notBuilder.SetPriority ((int)NotificationPriority.High);

				// Запуск
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
			isStarted = false;

			// Освобождение ресурсов
			ns.Dispose ();
			notBuilder.Dispose ();
			notManager.Dispose ();
			masterIntent.Dispose ();
			masterPendingIntent.Dispose ();

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
	/// Класс описывает запускаемое задание
	/// </summary>
	[Service (Name = "com.RD_AAOW.UniNotifierLink",
		Label = "UniNotifierLink", Icon = "@mipmap/icon")]
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
		protected override void OnHandleIntent (Android.Content.Intent intent)
			{
			Launcher.OpenAsync (NotificationsSupport.CurrentLink);
			}
		}
	}
