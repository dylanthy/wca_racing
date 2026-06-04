public static class CarSelectionRuntime
{
    public static CarDefinition SelectedCar { get; private set; }
    public static int SelectedIndex { get; private set; } = -1;

    public static void SetSelection(CarDefinition selectedCar, int selectedIndex)
    {
        SelectedCar = selectedCar;
        SelectedIndex = selectedIndex;
    }
}
