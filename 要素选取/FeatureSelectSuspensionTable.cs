using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMGI_Plugin_EmergencyMap
{
    public class FeatureSelectSuspensionTable
    {
        public FeatureSelectSuspensionWPF FeSelectSuFormResult;

        private FeatureSelectSuspensionTable()
        {
            FeSelectSuFormResult = new FeatureSelectSuspensionWPF();
        }

        private static FeatureSelectSuspensionTable m_instance = null;//实例

        public static FeatureSelectSuspensionTable Instance//获取实例
        {
            get
            {
                if (null == FeatureSelectSuspensionTable.m_instance)
                {
                    FeatureSelectSuspensionTable.m_instance = new FeatureSelectSuspensionTable();
                }

                return FeatureSelectSuspensionTable.m_instance;
            }
        }

        public bool WinFrmShow = false;

        public void Show()
        {
            if (FeSelectSuFormResult == null)
            {
                FeSelectSuFormResult = new FeatureSelectSuspensionWPF();
                FeSelectSuFormResult.Show();
            }
            else
            {
                if (FeSelectSuFormResult.IsLoaded)
                {
                    FeSelectSuFormResult = new FeatureSelectSuspensionWPF();
                    FeSelectSuFormResult.Show();
                }
                else
                {
                    FeSelectSuFormResult.Show();
                    FeSelectSuFormResult.Activate();
                }
            }
        }
        public void Hide()
        {
            if (FeSelectSuFormResult.IsLoaded)
            {
                FeSelectSuFormResult = new FeatureSelectSuspensionWPF();
                FeSelectSuFormResult.Show();
            }
            else
            {
                FeSelectSuFormResult.Activate();
            }
        }
    }
}
