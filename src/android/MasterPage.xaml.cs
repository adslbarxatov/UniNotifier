namespace RD_AAOW
	{
	/// <summary>
	/// Класс описывает главный макет приложения
	/// </summary>
	[XamlCompilation (XamlCompilationOptions.Compile)]
	public partial class MasterPage: NavigationPage
		{
		/// <summary>
		/// Конструктор. Создаёт макет приложения
		/// </summary>
		public MasterPage ()
			{
			InitializeComponent ();
			}

		// Направление повторного вызова на метод Resume в приложении (не работает в MAUI)
		protected override void OnAppearing ()
			{
			if (NotificationsSupport.AllowServiceToStart)
				appEx.ResumeApp ();

			base.OnAppearing ();
			}

		/// <summary>
		/// Возвращает или задаёт действующий экземпляр текущего приложенияж
		/// </summary>
		public static App AppEx
			{
			get
				{
				return appEx;
				}
			set
				{
				appEx = value;
				}
			}
		private static App appEx;
		}
	}
