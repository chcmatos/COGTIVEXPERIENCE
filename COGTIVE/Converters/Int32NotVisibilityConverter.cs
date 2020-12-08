namespace COGTIVE.Converters
{
    internal class Int32NotVisibilityConverter : Int32VisibilityConverter
    {
        protected override bool Compare(int valueInt, int paramInt)
        {
            return !base.Compare(valueInt, paramInt);
        }
    }
}
