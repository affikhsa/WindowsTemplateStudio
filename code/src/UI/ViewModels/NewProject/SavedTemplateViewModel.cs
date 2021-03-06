﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Input;
using System.Windows.Threading;

using Microsoft.Templates.Core.Mvvm;
using Microsoft.Templates.Core;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.Templates.UI.Services;

namespace Microsoft.Templates.UI.ViewModels.NewProject
{
    public class SavedTemplateViewModel : Observable
    {
        #region TemplatesProperties

        private ITemplateInfo _template;
#pragma warning disable SA1008 // Opening parenthesis must be spaced correctly - StyleCop can't handle Tuples
        public (string name, ITemplateInfo template) UserSelection => (ItemName, _template);
#pragma warning restore SA1008 // Opening parenthesis must be spaced correctly

        private string _identity;
        public string Identity
        {
            get => _identity;
            set => SetProperty(ref _identity, value);
        }

        private string _itemName;
        public string ItemName
        {
            get => _itemName;
            set
            {
                SetProperty(ref _itemName, value);
                OnPropertyChanged("DisplayText");
                ItemForeground = GetItemForeground(true);
                AuthorForeground = GetAuthorForeground(true);
                colorTimer.Start();
            }
        }
        private string _newItemName;
        public string NewItemName
        {
            get
            {
                return _newItemName;
            }
            set
            {
                SetProperty(ref _newItemName, value);
                if (CanChooseItemName)
                {
                    ValidateTemplateName?.Invoke(this);
                }
            }
        }

        private bool _canChooseItemName;
        public bool CanChooseItemName
        {
            get => _canChooseItemName;
            set => SetProperty(ref _canChooseItemName, value);
        }

        private bool _isHidden;
        public bool IsHidden
        {
            get => _isHidden;
            set => SetProperty(ref _isHidden, value);
        }

        private string _templateName;
        public string TemplateName
        {
            get => _templateName;
            set => SetProperty(ref _templateName, value);
        }

        private List<string> _dependencyList;
        public List<string> DependencyList
        {
            get => _dependencyList;
            set => SetProperty(ref _dependencyList, value);
        }

        private string _author;
        public string Author
        {
            get => _author;
            set => SetProperty(ref _author, value);
        }

        private int _genGroup;
        public int GenGroup
        {
            get => _genGroup;
            set => SetProperty(ref _genGroup, value);
        }

        private TemplateType _templateType;
        public TemplateType TemplateType
        {
            get => _templateType;
            set => SetProperty(ref _templateType, value);
        }

        private bool _isRemoveEnabled;
        public bool IsRemoveEnabled
        {
            get => _isRemoveEnabled;
            set => SetProperty(ref _isRemoveEnabled, value);
        }

        private bool _isHome;
        public bool IsHome
        {
            get => _isHome;
            set
            {
                SetProperty(ref _isHome, value);
                ItemFontWeight = value ? FontWeights.Bold : FontWeights.Normal;
                OnPropertyChanged("CanSetHome");
                MainViewModel.Current.FinishCommand.OnCanExecuteChanged();
            }
        }

        public bool CanSetHome
        {
            get
            {
                return !IsHome;
            }
        }
        #endregion

        #region UISummaryProperties
        private DispatcherTimer colorTimer = new DispatcherTimer() { Interval = TimeSpan.FromSeconds(2) };

        private bool _isEditionEnabled;
        public bool IsEditionEnabled
        {
            get => _isEditionEnabled;
            set
            {
                EditingContentVisibility = value ? Visibility.Visible : Visibility.Collapsed;
                NoEditingContentVisibility = value ? Visibility.Collapsed : Visibility.Visible;
                SetProperty(ref _isEditionEnabled, value);
            }
        }

        private Visibility _noEditingContentVisibility = Visibility.Visible;
        public Visibility NoEditingContentVisibility
        {
            get => _noEditingContentVisibility;
            private set => SetProperty(ref _noEditingContentVisibility, value);
        }

        private Visibility _editingContentVisibility = Visibility.Collapsed;
        public Visibility EditingContentVisibility
        {
            get => _editingContentVisibility;
            private set => SetProperty(ref _editingContentVisibility, value);
        }

        private bool _isValidName;
        public bool IsValidName
        {
            get => _isValidName;
            set => SetProperty(ref _isValidName, value);
        }

        private string _errorMessage;
        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        private FontWeight _itemFontWeight = FontWeights.Normal;
        public FontWeight ItemFontWeight
        {
            get => _itemFontWeight;
            set => SetProperty(ref _itemFontWeight, value);
        }

        private bool _allowDragAndDrop;
        public bool AllowDragAndDrop
        {
            get => _allowDragAndDrop;
            set => SetProperty(ref _allowDragAndDrop, value);
        }

        private bool _isOpen;
        public bool IsOpen
        {
            get => _isOpen;
            set => SetProperty(ref _isOpen, value);
        }

        private Brush _itemForeground;
        public Brush ItemForeground
        {
            get => _itemForeground;
            set => SetProperty(ref _itemForeground, value);
        }

        private Brush _authorForeground;
        public Brush AuthorForeground
        {
            get => _authorForeground;
            set => SetProperty(ref _authorForeground, value);
        }

        public string DisplayText => CanChooseItemName ? $"{ItemName} [{TemplateName}]" : ItemName;

        public ICommand OpenCommand { get; set; }

        public ICommand RemoveCommand { get; set; }

        public ICommand RenameCommand { get; set; }

        public ICommand ConfirmRenameCommand { get; set; }

        public Action<SavedTemplateViewModel> ValidateTemplateName;

        public ICommand _cancelRenameCommand;
        public ICommand CancelRenameCommand => _cancelRenameCommand ?? (_cancelRenameCommand = new RelayCommand(CancelRenameAction));

        public Action CancelRenameAction => OnCancelRename;
        #endregion

        public SavedTemplateViewModel((string name, ITemplateInfo template) item, bool isRemoveEnabled, ICommand openCommand, ICommand removeTemplateCommand, ICommand renameItemCommand, ICommand confirmRenameCommand, Action<SavedTemplateViewModel> validateCurrentTemplateName)
        {
            _template = item.template;
            colorTimer.Tick += OnColorTimerTick;
            ItemName = item.name;
            NewItemName = item.name;
            Author = item.template.Author;
            GenGroup = item.template.GetGenGroup();
            TemplateType = item.template.GetTemplateType();
            CanChooseItemName = item.template.GetItemNameEditable();
            Identity = item.template.Identity;
            TemplateName = item.template.Name;
            IsHidden = item.template.GetIsHidden();
            DependencyList = item.template.GetDependencyList();
            IsRemoveEnabled = isRemoveEnabled;
            ItemForeground = GetItemForeground(true);
            AuthorForeground = GetAuthorForeground(true);
            OpenCommand = openCommand;
            RemoveCommand = removeTemplateCommand;
            RenameCommand = renameItemCommand;
            ConfirmRenameCommand = confirmRenameCommand;
            ValidateTemplateName = validateCurrentTemplateName;
            AllowDragAndDrop = false;
        }

        private void OnColorTimerTick(object sender, EventArgs e)
        {
            ItemForeground = GetItemForeground(false);
            AuthorForeground = GetAuthorForeground(false);

            colorTimer.Stop();
        }

        internal void TryClose(bool force = false)
        {
            if (IsOpen)
            {
                 IsOpen = false;
            }
        }

        public void UpdateAllowDragAndDrop(int pagesCount)
        {
            AllowDragAndDrop = GenGroup == 0 && TemplateType == TemplateType.Page && pagesCount > 1;
        }

        public void TryReleaseHome()
        {
            if (_isHome)
            {
                IsHome = false;
            }
        }

        public void OnCancelRename()
        {
            if (IsEditionEnabled)
            {
                IsEditionEnabled = false;
                _newItemName = string.Empty;
                OnPropertyChanged("NewItemName");
                MainViewModel.Current.CleanStatus(true);
            }
        }

        private SolidColorBrush GetItemForeground(bool isNewAdded)
        {
            if (SystemService.Instance.IsHighContrast)
            {
                return SystemColors.ControlTextBrush;
            }
            else
            {
                if (isNewAdded)
                {
                    return MainViewModel.Current.MainView.FindResource("UIBlue") as SolidColorBrush;
                }
                else
                {
                    return MainViewModel.Current.MainView.FindResource("UIBlack") as SolidColorBrush;
                }
            }
        }

        private SolidColorBrush GetAuthorForeground(bool isNewAdded)
        {
            if (SystemService.Instance.IsHighContrast)
            {
                return SystemColors.ControlTextBrush;
            }
            else
            {
                if (isNewAdded)
                {
                    return MainViewModel.Current.MainView.FindResource("UIBlue") as SolidColorBrush;
                }
                else
                {
                    return MainViewModel.Current.MainView.FindResource("UIGray") as SolidColorBrush;
                }
            }
        }
    }
}
