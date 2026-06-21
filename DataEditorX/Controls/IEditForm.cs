namespace DataEditorX.Controls
{
    public interface IEditForm
    {
        //Get opened file path
        string GetOpenFile();
        //Create file
        bool Create(string file);
        //Open file
        bool Open(string file, string name);
        //Check whether a file can be opened
        bool CanOpen(string file);
        //Save
        bool Save(bool shift = false);
        //Mark as active window
        void SetActived();
    }
}
