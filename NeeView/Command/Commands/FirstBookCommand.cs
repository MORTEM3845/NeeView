using NeeView.Properties;

namespace NeeView
{
    public class FirstBookCommand : CommandElement
    {
        public FirstBookCommand()
        {
            this.Group = TextResources.GetString("CommandGroup.BookMove");
            this.IsShowMessage = false;
            this.PairPartner = "LastBook";
        }

        public override bool CanExecute(object? sender, CommandContext e)
        {
            return !NowLoading.Current.IsDisplayNowLoading;
        }

        public override void Execute(object? sender, CommandContext e)
        {
            _ = BookshelfFolderList.Current.FirstFolder();
        }
    }
}
