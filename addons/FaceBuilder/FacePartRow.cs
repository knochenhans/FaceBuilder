using Godot;

public partial class FacePartRow : Control
{
    [Export] public CheckBox CheckBox;
    [Export] public Button ButtonLeft;
    [Export] public Button ButtonRight;
    [Export] public Label LabelPartVariationName;
    [Export] public Label LabelPartName;

    [Signal] public delegate void PreviousPartEventHandler(string partName, string variationName);
    [Signal] public delegate void NextPartEventHandler(string partName, string variationName);
    [Signal] public delegate void TogglePartVisibilityEventHandler(string partName, bool visible);

    public override void _Ready()
    {
        CheckBox.Toggled += OnCheckBoxToggled;
        ButtonLeft.Pressed += OnLeftButtonPressed;
        ButtonRight.Pressed += OnRightButtonPressed;
    }

    public void Init(string partName)
    {
        LabelPartName.Text = partName;
    }

    private void OnCheckBoxToggled(bool pressed) => EmitSignal(SignalName.TogglePartVisibility, LabelPartName.Text, pressed);
    private void OnLeftButtonPressed() => EmitSignal(SignalName.PreviousPart, LabelPartName.Text, LabelPartVariationName.Text);
    private void OnRightButtonPressed() => EmitSignal(SignalName.NextPart, LabelPartName.Text, LabelPartVariationName.Text);
}
