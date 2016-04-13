﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using Abnormal_UI.UI;
using Abnormal_UI.UI.Test;

namespace Abnormal_UI
{
   
    public partial class MainWindow 
    {
        private AbnormalViewModel _abnormalModel;
        private SimpleBindViewModel _sbModel;
        public AbnormalAttackUserControl _abnormalAttackWindow { get; set; }
        public LsbAttackUserControl _lsbAttackWindow { get; set; }

        private TestViewModel _testModel;
        public TestUserControl _testWindow { get; set; }

        public MainWindow()
        {
            InitializeComponent();
           
            _abnormalModel = new AbnormalViewModel();
            _abnormalModel.PopulateModel();
            _abnormalAttackWindow = new AbnormalAttackUserControl(_abnormalModel);

            _sbModel = new SimpleBindViewModel();
            _sbModel.PopulateModel();
            _lsbAttackWindow = new LsbAttackUserControl(_sbModel);

            _testModel = new TestViewModel();
            _testModel.PopulateModel();
            _testWindow = new TestUserControl(_testModel);

            DataContext = this;
        }

        private void Root_MouseDown(object sender, MouseButtonEventArgs eventArgs)
        {
            if (eventArgs.ChangedButton == MouseButton.Left)
            {
                DragMove();
            }
        }
        private void CloseButton_Click(object sender, RoutedEventArgs eventArgs)
        {
            Close();
        }

    }
}
