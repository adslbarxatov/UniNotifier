using System;
using System.Threading;
using System.Windows.Forms;

namespace RD_AAOW
	{
	/// <summary>
	/// Класс описывает точку входа приложения
	/// </summary>
	public static class UniNotifierProgram
		{
		/// <summary>
		/// Главная точка входа для приложения
		/// </summary>
		[STAThread]
		public static void Main ()
			{
			// Инициализация
			Application.EnableVisualStyles ();
			Application.SetCompatibleTextRenderingDefault (false);

			// Проверка запуска единственной копии
			bool result;
			Mutex instance = new Mutex (true, ProgramDescription.AssemblyTitle, out result);
			if (!result)
				{
				MessageBox.Show (string.Format (Localization.GetText ("AlreadyStarted", Localization.CurrentLanguage),
					ProgramDescription.AssemblyTitle),
					ProgramDescription.AssemblyVisibleName, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return;
				}

			// Отображение справки и запроса на принятие Политики
			if (!ProgramDescription.AcceptEULA ())
				return;
			ProgramDescription.ShowAbout (true);

			// Запуск
			Application.Run (new UniNotifierForm ());
			}
		}
	}
