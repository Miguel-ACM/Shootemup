public sealed class DifficultyOption : AMenuElement
{
    NewGame owner;
    int option;
    public void Configure(NewGame owner, int option) { this.owner = owner; this.option = option; }
    public override void OnAccept() { owner.Choose(option); }
}
