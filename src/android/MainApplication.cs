using Android.App;
using Android.Runtime;

namespace RD_AAOW
	{
#if DEBUG
	[Application (Debuggable = true)]
#else
	[Application (Debuggable = false)]
#endif
	public class MainApplication: MauiApplication
		{
		/// <summary>
		/// Конструктор экземпляра приложения
		/// </summary>
		public MainApplication (IntPtr handle, JniHandleOwnership ownership) : base (handle, ownership)
			{
			}

		// Переопределение события создания экземпляра программы
		protected override MauiApp CreateMauiApp ()
			{
			return MauiProgram.CreateMauiApp ();
			}
		}
	}
