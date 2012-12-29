using System.Windows.Forms;
using JetBrains.ActionManagement;
using JetBrains.Application.DataContext;

namespace Unity.TypedFactories.ReSharperFindUsages
{
  [ActionHandler("Unity.TypedFactories.ReSharperFindUsages.About")]
  public class AboutAction : IActionHandler
  {
    public bool Update(IDataContext context, ActionPresentation presentation, DelegateUpdate nextUpdate)
    {
      // return true or false to enable/disable this action
      return true;
    }

    public void Execute(IDataContext context, DelegateExecute nextExecute)
    {
      MessageBox.Show(
        "Unity.TypedFactories.ReSharperFindUsages\nTesteroids\n\nHelps navigating to the instantiation of classes created by Unity.TypedFactories",
        "About Unity.TypedFactories.ReSharperFindUsages",
        MessageBoxButtons.OK,
        MessageBoxIcon.Information);
    }
  }
}
