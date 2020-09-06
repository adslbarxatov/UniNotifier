using System;
using System.Windows.Forms;

namespace RD_AAOW
	{
	/// <summary>
	/// Класс описывает форму настроек оповещений
	/// </summary>
	public partial class SettingsForm:Form
		{
		// Переменные и константы
		private NotificationsSet notifications;
		private SupportedLanguages al = Localization.CurrentLanguage;

		/// <summary>
		/// Конструктор. Настраивает главную форму приложения
		/// </summary>
		/// <param name="Notifications">Набор загруженных оповещений</param>
		/// <param name="UpdatingFrequencyStep">Шаг изменения частоты обновления</param>
		public SettingsForm (NotificationsSet Notifications, uint UpdatingFrequencyStep)
			{
			// Инициализация
			InitializeComponent ();
			this.Text = ProgramDescription.AssemblyTitle;
			this.CancelButton = BClose;

			LanguageCombo.Items.AddRange (Localization.LanguagesNames);
			try
				{
				LanguageCombo.SelectedIndex = (int)al;
				}
			catch
				{
				LanguageCombo.SelectedIndex = 0;
				}

			for (uint i = 1; i <= 24; i++)
				FrequencyCombo.Items.Add ((i * UpdatingFrequencyStep).ToString ());
			FrequencyCombo.SelectedIndex = 2;
			EnabledCheck.Checked = true;

			NameText.MaxLength = BeginningText.MaxLength = EndingText.MaxLength = Notification.MaxBeginningEndingLength;

			// Загрузка оповещений в список
			notifications = Notifications;

			UpdateButtons ();

			for (int i = 0; i < notifications.Notifications.Count; i++)
				NotificationsList.Items.Add (notifications.Notifications[i].Name +
					(notifications.Notifications[i].IsEnabled ? " (+)" : " (–)"));
			if (NotificationsList.Items.Count > 0)
				NotificationsList.SelectedIndex = 0;

			// Загрузка шаблонов
			for (uint i = 0; i < notifications.NotificationsTemplates.TemplatesCount; i++)
				TemplatesCombo.Items.Add (notifications.NotificationsTemplates.GetName (i));
			TemplatesCombo.SelectedIndex = 0;

			// Запуск
			this.ShowDialog ();
			}

		// Обновление состояния кнопок
		private void UpdateButtons ()
			{
			BAdd.Enabled = (notifications.Notifications.Count < NotificationsSet.MaxNotifications);
			BDelete.Enabled = BUpdate.Enabled = (notifications.Notifications.Count > 1);	// Одно должно остаться
			}

		// Закрытие окна просмотра
		private void BClose_Click (object sender, EventArgs e)
			{
			// Сохранение оповещений
			notifications.SaveNotifications ();

			// Закрытие окна
			this.Close ();
			}

		// Загрузка значений в поля
		private void NotificationsList_DoubleClick (object sender, EventArgs e)
			{
			// Контроль
			if (NotificationsList.SelectedIndex < 0)
				return;

			// Загрузка
			NameText.Text = notifications.Notifications[NotificationsList.SelectedIndex].Name;
			LinkText.Text = notifications.Notifications[NotificationsList.SelectedIndex].Link;
			BeginningText.Text = notifications.Notifications[NotificationsList.SelectedIndex].Beginning;
			EndingText.Text = notifications.Notifications[NotificationsList.SelectedIndex].Ending;
			FrequencyCombo.SelectedIndex = (int)notifications.Notifications[NotificationsList.SelectedIndex].UpdateFrequency - 1;
			EnabledCheck.Checked = notifications.Notifications[NotificationsList.SelectedIndex].IsEnabled;
			}

		// Добавление и обновление позиций
		private void BAdd_Click (object sender, EventArgs e)
			{
			// Добавление
			UpdateItem (-1);

			// Обновление кнопок
			UpdateButtons ();
			}

		private void BUpdate_Click (object sender, EventArgs e)
			{
			if (MessageBox.Show (Localization.GetText ("UpdateMessage", al), ProgramDescription.AssemblyTitle,
				MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
				return;

			UpdateItem (NotificationsList.SelectedIndex);
			}

		// Метод обновления оповещений (номер -1 – добавление нового)
		private void UpdateItem (int ItemNumber)
			{
			// Инициализация оповещения
			Notification ni = new Notification (NameText.Text, LinkText.Text, BeginningText.Text, EndingText.Text,
				(uint)(FrequencyCombo.SelectedIndex + 1));
			if (!ni.IsInited)
				{
				MessageBox.Show (Localization.GetText ("NotEnoughDataMessage", al),
					ProgramDescription.AssemblyTitle, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return;
				}
			ni.IsEnabled = EnabledCheck.Checked;

			// Добавление
			if (ItemNumber < 0)
				{
				NotificationsList.Items.Add (ni.Name + (ni.IsEnabled ? " (+)" : " (–)"));
				notifications.Notifications.Add (ni);
				}
			else if (ItemNumber < NotificationsList.Items.Count)
				{
				NotificationsList.Items[ItemNumber] = ni.Name + (ni.IsEnabled ? " (+)" : " (–)");
				notifications.Notifications[ItemNumber] = ni;
				}
			else
				{
				MessageBox.Show (Localization.GetText ("UpdateLineNotSpecified", al),
					ProgramDescription.AssemblyTitle, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return;
				}
			}

		// Удаление оповещения
		private void BDelete_Click (object sender, EventArgs e)
			{
			// Контроль
			if (NotificationsList.SelectedIndex < 0)
				{
				MessageBox.Show (Localization.GetText ("DeleteLineNotSpecified", al),
					ProgramDescription.AssemblyTitle, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return;
				}

			if (MessageBox.Show (Localization.GetText ("DeleteMessage", al), ProgramDescription.AssemblyTitle,
				MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) == DialogResult.No)
				return;

			// Удаление
			notifications.Notifications.RemoveAt (NotificationsList.SelectedIndex);
			NotificationsList.Items.RemoveAt (NotificationsList.SelectedIndex);

			// Обновление кнопок
			UpdateButtons ();
			}

		// Загрузка шаблона в поля
		private void BLoadTemplate_Click (object sender, EventArgs e)
			{
			// Проверка
			if (notifications.NotificationsTemplates.IsTemplateIncomplete ((uint)TemplatesCombo.SelectedIndex))
				MessageBox.Show (Localization.GetText ("CurlyTemplate", al), ProgramDescription.AssemblyTitle,
					MessageBoxButtons.OK, MessageBoxIcon.Information);

			// Заполнение
			NameText.Text = notifications.NotificationsTemplates.GetName ((uint)TemplatesCombo.SelectedIndex);
			LinkText.Text = notifications.NotificationsTemplates.GetLink ((uint)TemplatesCombo.SelectedIndex);
			BeginningText.Text = notifications.NotificationsTemplates.GetBeginning ((uint)TemplatesCombo.SelectedIndex);
			EndingText.Text = notifications.NotificationsTemplates.GetEnding ((uint)TemplatesCombo.SelectedIndex);
			}

		// Автоматизированный поиск ограничителей
		private void FindDelimiters_Click (object sender, EventArgs e)
			{
			// Контроль
			if (BeginningText.Text == "")
				{
				MessageBox.Show (Localization.GetText ("KeywordNotSpecified", al),
					ProgramDescription.AssemblyTitle, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return;
				}

			// Поиск
			string beginning = "", ending = "";
			if (!Notification.FindDelimiters (LinkText.Text, BeginningText.Text, out beginning, out ending))
				{
				MessageBox.Show (Localization.GetText ("SearchFailure", al),
					ProgramDescription.AssemblyTitle, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return;
				}

			// Успешно
			BeginningText.Text = beginning.Trim ();
			EndingText.Text = ending.Trim ();
			}

		// Локализация формы
		private void LanguageCombo_SelectedIndexChanged (object sender, EventArgs e)
			{
			// Сохранение
			al = Localization.CurrentLanguage = (SupportedLanguages)LanguageCombo.SelectedIndex;

			// Локализация
			Localization.SetControlsText (this, al);
			}
		}
	}
