using System;
using System.Windows.Forms;

namespace RD_AAOW
	{
	/// <summary>
	/// Класс описывает форму настроек оповещений
	/// </summary>
	public partial class SettingsForm: Form
		{
		// Переменные и константы
		private NotificationsSet notifications;
		private SupportedLanguages al = Localization.CurrentLanguage;
		private uint updatingFrequencyStep;

		/// <summary>
		/// Конструктор. Настраивает главную форму приложения
		/// </summary>
		/// <param name="Notifications">Набор загруженных оповещений</param>
		/// <param name="UpdatingFrequencyStep">Шаг изменения частоты обновления</param>
		public SettingsForm (NotificationsSet Notifications, uint UpdatingFrequencyStep)
			{
			// Инициализация
			InitializeComponent ();
			notifications = Notifications;
			updatingFrequencyStep = UpdatingFrequencyStep;

			this.Text = ProgramDescription.AssemblyVisibleName;
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

			OccurrenceField.Minimum = 1;
			OccurrenceField.Maximum = Notification.MaxOccurrenceNumber;

			NameText.MaxLength = BeginningText.MaxLength = EndingText.MaxLength = Notification.MaxBeginningEndingLength;

			// Загрузка оповещений в список
			UpdateButtons ();

			for (int i = 0; i < notifications.Notifications.Count; i++)
				NotificationsList.Items.Add (notifications.Notifications[i].Name +
					(notifications.Notifications[i].IsEnabled ? " (+)" : " (–)"));
			if (NotificationsList.Items.Count > 0)
				NotificationsList.SelectedIndex = 0;

			// Запуск
			ProgramDescription.ShowTips (ProgramDescription.TipTypes.StartupTip);
			this.ShowDialog ();
			}

		// Обновление состояния кнопок
		private void UpdateButtons ()
			{
			BAdd.Enabled = NotWizard.Enabled = (notifications.Notifications.Count < NotificationsSet.MaxNotifications);
			BDelete.Enabled = BUpdate.Enabled = (notifications.Notifications.Count > 1);    // Одно должно остаться
			}

		// Закрытие окна просмотра
		private void BClose_Click (object sender, EventArgs e)
			{
			// Сохранение оповещений
			notifications.SaveNotifications ();

			// Закрытие окна
			ProgramDescription.ShowTips (ProgramDescription.TipTypes.ServiceLaunchTip);
			this.Close ();
			}

		// Загрузка значений в поля
		private void NotificationsList_Select (object sender, EventArgs e)
			{
			// Контроль
			if (NotificationsList.SelectedIndex < 0)
				return;
			else if (NotificationsList.SelectedIndex != 0)
				ProgramDescription.ShowTips (ProgramDescription.TipTypes.CurrentNotButton);

			// Загрузка
			int i = NotificationsList.SelectedIndex;
			NameText.Text = notifications.Notifications[i].Name;
			LinkText.Text = notifications.Notifications[i].Link;
			BeginningText.Text = notifications.Notifications[i].Beginning;
			EndingText.Text = notifications.Notifications[i].Ending;
			FrequencyCombo.SelectedIndex = (int)notifications.Notifications[i].UpdateFrequency - 1;
			EnabledCheck.Checked = notifications.Notifications[i].IsEnabled;
			OccurrenceField.Value = notifications.Notifications[i].OccurrenceNumber;
			}

		// Добавление и обновление позиций
		private void BAdd_Click (object sender, EventArgs e)
			{
			// Добавление
			ProgramDescription.ShowTips (ProgramDescription.TipTypes.AddButton);
			UpdateItem (-1);

			// Обновление кнопок
			UpdateButtons ();
			}

		private void BUpdate_Click (object sender, EventArgs e)
			{
			ProgramDescription.ShowTips (ProgramDescription.TipTypes.ApplyButton);
			UpdateItem (NotificationsList.SelectedIndex);
			}

		// Метод обновления оповещений (номер -1 – добавление нового)
		private void UpdateItem (int ItemNumber)
			{
			// Инициализация оповещения
			Notification ni = new Notification (NameText.Text, LinkText.Text, BeginningText.Text, EndingText.Text,
				(uint)(FrequencyCombo.SelectedIndex + 1), (uint)OccurrenceField.Value);

			if (!ni.IsInited)
				{
				MessageBox.Show (Localization.GetText ("NotEnoughDataMessage", al),
					ProgramDescription.AssemblyVisibleName, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return;
				}
			if ((ItemNumber < 0) && notifications.Notifications.Contains (ni))
				{
				MessageBox.Show (Localization.GetText ("NotMatchingNames", al),
					ProgramDescription.AssemblyVisibleName, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return;
				}

			ni.IsEnabled = EnabledCheck.Checked;

			// Добавление
			if (ItemNumber < 0)
				{
				notifications.Notifications.Add (ni);
				NotificationsList.Items.Add (ni.Name + (ni.IsEnabled ? " (+)" : " (–)"));
				}
			else if (ItemNumber < NotificationsList.Items.Count)
				{
				notifications.Notifications[ItemNumber] = ni;
				NotificationsList.Items[ItemNumber] = ni.Name + (ni.IsEnabled ? " (+)" : " (–)");
				}
			else
				{
				MessageBox.Show (Localization.GetText ("UpdateLineNotSpecified", al),
					ProgramDescription.AssemblyVisibleName, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return;
				}
			}

		// Удаление оповещения
		private void BDelete_Click (object sender, EventArgs e)
			{
			// Контроль
			ProgramDescription.ShowTips (ProgramDescription.TipTypes.DeleteButton);
			if (NotificationsList.SelectedIndex < 0)
				{
				MessageBox.Show (Localization.GetText ("DeleteLineNotSpecified", al),
					ProgramDescription.AssemblyVisibleName, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return;
				}

			if (MessageBox.Show (Localization.GetText ("DeleteMessage", al), ProgramDescription.AssemblyVisibleName,
				MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) == DialogResult.No)
				return;

			// Удаление
			notifications.Notifications.RemoveAt (NotificationsList.SelectedIndex);
			NotificationsList.Items.RemoveAt (NotificationsList.SelectedIndex);

			// Обновление кнопок
			UpdateButtons ();
			}

		// Локализация формы
		private void LanguageCombo_SelectedIndexChanged (object sender, EventArgs e)
			{
			// Сохранение
			al = Localization.CurrentLanguage = (SupportedLanguages)LanguageCombo.SelectedIndex;

			// Локализация
			Localization.SetControlsText (this, al);
			}

		// Подсказка по полю Occurence
		private void OccurrenceField_Click (object sender, EventArgs e)
			{
			ProgramDescription.ShowTips (ProgramDescription.TipTypes.OccurenceTip);
			}

		// Выгрузка настроек в буфер обмена
		private void ShareSettings_Click (object sender, EventArgs e)
			{
			// Подсказка
			ProgramDescription.ShowTips (ProgramDescription.TipTypes.ShareSettings);

			// Копирование
			try
				{
				Clipboard.SetText (NameText.Text + NotificationsTemplatesProvider.ClipboardTemplateSplitter[0].ToString () +
					LinkText.Text + NotificationsTemplatesProvider.ClipboardTemplateSplitter[0].ToString () +
					BeginningText.Text + NotificationsTemplatesProvider.ClipboardTemplateSplitter[0].ToString () +
					EndingText.Text + NotificationsTemplatesProvider.ClipboardTemplateSplitter[0].ToString () +
					((uint)(OccurrenceField.Value)).ToString ());
				}
			catch { }
			}

		// Вызов мастера оповещений
		private void NotWizard_Click (object sender, EventArgs e)
			{
			// Запрос
			WizardForm wf = new WizardForm (notifications, al, updatingFrequencyStep, (uint)FrequencyCombo.Items.Count);

			// Обновление
			if (wf.Cancelled)
				return;

			NameText.Text = wf.NotificationName;
			LinkText.Text = wf.NotificationLink;
			BeginningText.Text = wf.NotificationBeginning;
			EndingText.Text = wf.NotificationEnding;
			FrequencyCombo.SelectedIndex = wf.UpdateFrequenciesListIndex;
			OccurrenceField.Value = wf.NotificationOccurrence;
			EnabledCheck.Checked = true;

			UpdateItem (-1);
			UpdateButtons ();
			}
		}
	}
