"""Templates of the busy veil, target bar, description list and auto form (appended to Controls.axaml)."""

def build():
    return '''
    <!-- ===================================================================== Busy -->

    <ControlTheme x:Key="{x:Type h:HelixBusy}" TargetType="h:HelixBusy">
        <Setter Property="Template">
            <ControlTemplate>
                <Panel>
                    <ContentPresenter Name="PART_ContentPresenter" Content="{TemplateBinding Content}" ContentTemplate="{TemplateBinding ContentTemplate}"
                                      IsEnabled="{Binding !IsVeilShown, RelativeSource={RelativeSource TemplatedParent}}" />
                    <Border Background="{DynamicResource Helix.Surface}" Opacity="0.82" CornerRadius="12" IsVisible="{TemplateBinding IsVeilShown}" />
                    <StackPanel HorizontalAlignment="Center" VerticalAlignment="Center" IsVisible="{TemplateBinding IsVeilShown}">
                        <h:HelixSpinner Size="26" HorizontalAlignment="Center" />
                        <TextBlock Text="{TemplateBinding Text}" Margin="0,10,0,0" FontSize="13" FontWeight="SemiBold" HorizontalAlignment="Center"
                                   Foreground="{DynamicResource Helix.TextMuted}" IsVisible="{TemplateBinding Text, Converter={x:Static StringConverters.IsNotNullOrEmpty}}" />
                    </StackPanel>
                </Panel>
            </ControlTemplate>
        </Setter>
    </ControlTheme>

    <!-- ===================================================================== Target bar -->

    <ControlTheme x:Key="{x:Type h:HelixTargetBar}" TargetType="h:HelixTargetBar">
        <Setter Property="FontFamily" Value="{DynamicResource Helix.FontFamily}" />
        <Setter Property="Template">
            <ControlTemplate>
                <StackPanel>
                    <DockPanel Margin="0,0,0,4">
                        <StackPanel DockPanel.Dock="Right" Orientation="Horizontal" Spacing="6" IsVisible="{TemplateBinding ShowValues}">
                            <TextBlock Text="{TemplateBinding ValueText}" FontSize="13" FontWeight="SemiBold" Foreground="{DynamicResource Helix.Text}" />
                            <TextBlock Text="{TemplateBinding TargetText}" FontSize="13" Foreground="{DynamicResource Helix.TextSubtle}" />
                            <TextBlock Name="Delta" Text="{TemplateBinding DeltaText}" FontSize="12" FontWeight="SemiBold" VerticalAlignment="Bottom" Foreground="{DynamicResource Helix.TextMuted}" />
                        </StackPanel>
                        <TextBlock Text="{TemplateBinding Label}" FontSize="13" FontWeight="Medium" Foreground="{DynamicResource Helix.TextMuted}" />
                    </DockPanel>
                    <h:HelixTargetTrack Name="PART_Track" />
                </StackPanel>
            </ControlTemplate>
        </Setter>
        <Style Selector="^:ok /template/ TextBlock#Delta"><Setter Property="Foreground" Value="{DynamicResource Helix.Success}" /></Style>
        <Style Selector="^:warn /template/ TextBlock#Delta"><Setter Property="Foreground" Value="{DynamicResource Helix.Warning}" /></Style>
        <Style Selector="^:off /template/ TextBlock#Delta"><Setter Property="Foreground" Value="{DynamicResource Helix.Danger}" /></Style>
    </ControlTheme>

    <!-- ===================================================================== Description list -->

    <ControlTheme x:Key="{x:Type h:HelixDescriptionList}" TargetType="h:HelixDescriptionList">
        <Setter Property="ItemsPanel"><ItemsPanelTemplate><h:HelixDescriptionPanel /></ItemsPanelTemplate></Setter>
        <Setter Property="Template">
            <ControlTemplate><ItemsPresenter Name="PART_ItemsPresenter" ItemsPanel="{TemplateBinding ItemsPanel}" /></ControlTemplate>
        </Setter>
    </ControlTheme>

    <ControlTheme x:Key="{x:Type h:HelixDescription}" TargetType="h:HelixDescription">
        <Setter Property="FontFamily" Value="{DynamicResource Helix.FontFamily}" />
        <Setter Property="FontSize" Value="14" />
        <Setter Property="Foreground" Value="{DynamicResource Helix.Text}" />
        <Setter Property="Template">
            <ControlTemplate>
                <Border Name="Line" BorderBrush="{DynamicResource Helix.Border}" BorderThickness="0">
                    <DockPanel>
                        <StackPanel Name="LabelRow" DockPanel.Dock="Top" Orientation="Horizontal" Margin="0,0,0,3">
                            <h:HelixIcon Icon="{TemplateBinding Icon}" Size="14" Margin="0,0,6,0" VerticalAlignment="Center" Foreground="{DynamicResource Helix.TextMuted}"
                                         IsVisible="{TemplateBinding Icon, Converter={x:Static ObjectConverters.IsNotNull}}" />
                            <TextBlock Text="{TemplateBinding Label}" FontSize="12.5" FontWeight="Medium" Foreground="{DynamicResource Helix.TextMuted}" TextWrapping="Wrap" />
                        </StackPanel>
                        <ContentPresenter Name="PART_ContentPresenter" Content="{TemplateBinding Content}" ContentTemplate="{TemplateBinding ContentTemplate}" VerticalAlignment="Top">
                            <ContentPresenter.Styles><Style Selector="TextBlock"><Setter Property="TextWrapping" Value="Wrap" /></Style></ContentPresenter.Styles>
                        </ContentPresenter>
                    </DockPanel>
                </Border>
            </ControlTemplate>
        </Setter>
        <Style Selector="^[IsInline=True] /template/ StackPanel#LabelRow">
            <Setter Property="DockPanel.Dock" Value="Left" />
            <Setter Property="Width" Value="{Binding LabelWidth, RelativeSource={RelativeSource TemplatedParent}}" />
            <Setter Property="Margin" Value="0,2,12,0" />
        </Style>
        <Style Selector="^[IsBordered=True] /template/ Border#Line">
            <Setter Property="BorderThickness" Value="0,0,0,1" />
            <Setter Property="Padding" Value="0,0,0,10" />
        </Style>
    </ControlTheme>

    <!-- ===================================================================== Auto form -->

    <ControlTheme x:Key="{x:Type h:HelixAutoForm}" TargetType="h:HelixAutoForm">
        <Setter Property="Template">
            <ControlTemplate>
                <StackPanel>
                    <h:HelixAutoFormPanel Name="PART_Fields" />
                    <StackPanel Orientation="Horizontal" HorizontalAlignment="Right" Margin="0,20,0,0" Spacing="8" IsVisible="{TemplateBinding ShowActions}">
                        <h:HelixButton Name="PART_Cancel" Content="{TemplateBinding CancelText}" Variant="Ghost" Tone="Neutral" IsVisible="{TemplateBinding ShowCancel}" />
                        <h:HelixButton Name="PART_Submit" Content="{TemplateBinding SubmitText}" Icon="check" IsDefault="True" />
                    </StackPanel>
                </StackPanel>
            </ControlTemplate>
        </Setter>
    </ControlTheme>
'''
