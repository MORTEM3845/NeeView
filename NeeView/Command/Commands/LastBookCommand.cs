using NeeView.Properties;

namespace NeeView
{
    public class LastBookCommand : CommandElement
    {
        public LastBookCommand()
        {
            this.Group = TextResources.GetString("CommandGroup.BookMove");
            this.IsShowMessage = false;
            this.PairPartner = "FirstBook";
        }

        public override bool CanExecute(object? sender, CommandContext e)
        {
            return !NowLoading.Current.IsDisplayNowLoading;
        }

        public override void Execute(object? sender, CommandContext e)
        {
            _ = BookshelfFolderList.Current.LastFolder();
        }
    }
}
