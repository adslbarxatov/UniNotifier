using IWshRuntimeLibrary;
using System;
using System.IO;

namespace RD_AAOW
	{
	/// <summary>
	/// Класс описывает функционал для создания ярлыков к приложениям Windows
	/// </summary>
	public static class WindowsShortcut
		{
		/// <summary>
		/// Метод создаёт ярлык к файлу на текущем рабочем столе
		/// </summary>
		/// <param name="ShortcutArguments">Аргументы командной строки, передаваемые с файлом</param>
		/// <param name="ShortcutFileName">Имя файла ярлыка</param>
		/// <param name="TargetFile">Файл, для которого создаётся ярлык</param>
		/// <returns>Возвращает 0 в случае успеха</returns>
		public static int CreateDesktopShortcut (string TargetFile, string ShortcutFileName, string ShortcutArguments)
			{
			return CreateShortcut (TargetFile, ShortcutFileName, Environment.GetFolderPath (Environment.SpecialFolder.Desktop),
				ShortcutArguments);
			}

		/// <summary>
		/// Метод создаёт ярлык к файлу в меню автозапуска
		/// </summary>
		/// <param name="ShortcutArguments">Аргументы командной строки, передаваемые с файлом</param>
		/// <param name="ShortcutFileName">Имя файла ярлыка</param>
		/// <param name="TargetFile">Файл, для которого создаётся ярлык</param>
		/// <returns>Возвращает 0 в случае успеха</returns>
		public static int CreateStartupShortcut (string TargetFile, string ShortcutFileName, string ShortcutArguments)
			{
			return CreateShortcut (TargetFile, ShortcutFileName, Environment.GetFolderPath (Environment.SpecialFolder.CommonStartup),
				ShortcutArguments);
			}

		/// <summary>
		/// Метод создаёт ярлык к файлу
		/// </summary>
		/// <param name="ShortcutArguments">Аргументы командной строки, передаваемые с файлом</param>
		/// <param name="ShortcutFileName">Имя файла ярлыка</param>
		/// <param name="TargetFile">Файл, для которого создаётся ярлык</param>
		/// <param name="ShortcutFilePath">Расположение файла ярлыка</param>
		/// <returns>Возвращает 0 в случае успеха;
		/// -1, если переданы пустые строки в качестве путей;
		/// -2, если файл, для которого создаётся ярлык, недоступен;
		/// -3, если ярлык с заданными параметрами не может быть создан (целевой путь некорректен или недоступен)</returns>
		public static int CreateShortcut (string TargetFile, string ShortcutFileName, string ShortcutFilePath,
			string ShortcutArguments)
			{
			// Контроль
			if (string.IsNullOrEmpty (TargetFile) || string.IsNullOrEmpty (ShortcutFileName) || string.IsNullOrEmpty (ShortcutFilePath))
				return -1;

			if (!System.IO.File.Exists (TargetFile))
				return -2;

			string filePath = ShortcutFilePath.EndsWith ("\\") ? ShortcutFilePath : (ShortcutFilePath + "\\");
			filePath += (ShortcutFileName + ".lnk");

			try
				{
				// Создание объектов
				WshShell shell = new WshShell ();
				IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut (filePath);

				// Настройка
				//shortcut.Description = "New shortcut for a Notepad";
				//shortcut.Hotkey = "Ctrl+Shift+N";
				if (!string.IsNullOrEmpty (ShortcutArguments))
					shortcut.Arguments = ShortcutArguments;

				shortcut.TargetPath = TargetFile;
				shortcut.WorkingDirectory = Path.GetDirectoryName (TargetFile);

				// Сохранение
				shortcut.Save ();
				}
			catch
				{
				return -3;
				}

			// Успешно
			return 0;
			}
		}
	}
