"""Templates of the filter bar, color picker and file drop (appended to Controls.axaml)."""
from gen_fields import picker

TONES = ["Neutral", "Success", "Warning", "Danger", "Info"]

def filter_theme(type_name, width, body, margin="0,0,10,8", extra=""):
    width_setter = f'\n        <Setter Property="Width" Value="{width}" />' if width else ""
    return f'''    <ControlTheme x:Key="{{x:Type h:{type_name}}}" TargetType="h:{type_name}">{width_setter}
        <Setter Property="Margin" Value="{margin}" />{extra}
        <Setter Property="Template"><ControlTemplate>{body}</ControlTemplate></Setter>
    </ControlTheme>
'''

COLOR_VALUE = '''<StackPanel Orientation="Horizontal" Spacing="10" VerticalAlignment="Center">
                                            <Border Width="22" Height="22" CornerRadius="6" BorderThickness="1" BorderBrush="#28000000" Background="{TemplateBinding SwatchBrush}"
                                                    IsVisible="{TemplateBinding HasValue}" />
                                            <TextBlock Text="{TemplateBinding Hex}" FontFamily="Cascadia Mono, Menlo, Consolas, monospace" VerticalAlignment="Center" />
                                            <TextBlock Text="{TemplateBinding Placeholder}" Foreground="{DynamicResource Helix.TextSubtle}" VerticalAlignment="Center"
                                                       IsVisible="{TemplateBinding HasValue, Converter={x:Static BoolConverters.Not}}" />
                                        </StackPanel>'''

def build():
    tones = "\n".join(f'        <Style Selector="^[Tone={t}]"><Setter Property="Accent" Value="{{DynamicResource Helix.{t}}}" /></Style>' for t in TONES)
    return '''
    <!-- ===================================================================== Filter bar -->

    <ControlTheme x:Key="{x:Type h:HelixFilterBar}" TargetType="h:HelixFilterBar">
        <Setter Property="FontFamily" Value="{DynamicResource Helix.FontFamily}" />
        <Setter Property="ItemsPanel"><ItemsPanelTemplate><WrapPanel /></ItemsPanelTemplate></Setter>
        <Setter Property="Template">
            <ControlTemplate>
                <DockPanel>
                    <StackPanel DockPanel.Dock="Right" Orientation="Horizontal" VerticalAlignment="Top" Margin="8,2,0,0" Spacing="6">
                        <h:HelixButton Name="PART_Clear" Content="{x:Static h:HelixFilterBar.ClearText}" Icon="x" Variant="Ghost" Tone="Neutral" Size="Sm"
                                       IsVisible="{TemplateBinding ActiveCount, Converter={x:Static h:HelixConverters.IsPositive}}" />
                        <h:HelixButton Name="PART_Saved" Content="{TemplateBinding SavedText}" Icon="star" IconEnd="chevron-down" Variant="Outline" Tone="Neutral" Size="Sm" />
                        <Popup Name="PART_SavePopup" PlacementTarget="{Binding #PART_Saved}" Placement="BottomEdgeAlignedRight" IsLightDismissEnabled="True" VerticalOffset="4">
                            <Border Margin="0,0,12,12" Padding="14" Width="280" Background="{DynamicResource Helix.Surface}" BorderBrush="{DynamicResource Helix.Border}"
                                    BorderThickness="1" CornerRadius="14" BoxShadow="0 12 32 0 #26000000">
                                <StackPanel Spacing="8">
                                    <TextBlock Text="{x:Static h:HelixFilterBar.NameText}" FontSize="13" FontWeight="SemiBold" Foreground="{DynamicResource Helix.Text}" />
                                    <TextBox Name="PART_SaveName" MinHeight="34" Padding="10,6" VerticalContentAlignment="Center" CornerRadius="8" />
                                    <h:HelixButton Name="PART_SaveConfirm" Content="{x:Static h:HelixFilterBar.SaveText}" Icon="check" Size="Sm" HorizontalAlignment="Right" />
                                </StackPanel>
                            </Border>
                        </Popup>
                    </StackPanel>
                    <ItemsPresenter Name="PART_ItemsPresenter" ItemsPanel="{TemplateBinding ItemsPanel}" />
                </DockPanel>
            </ControlTemplate>
        </Setter>
    </ControlTheme>

''' + filter_theme("HelixTextFilter", 240, '''<h:HelixTextField Icon="search" IsClearable="True" Placeholder="{TemplateBinding Placeholder}"
                                Text="{Binding Text, RelativeSource={RelativeSource TemplatedParent}, Mode=TwoWay}" />''') \
      + filter_theme("HelixChoiceFilter", 220, '''<h:HelixMultiSelect ItemsSource="{TemplateBinding Choices}" SelectedItems="{Binding Chosen, RelativeSource={RelativeSource TemplatedParent}, Mode=TwoWay}"
                                  Placeholder="{TemplateBinding Label}" IsSearchable="{TemplateBinding IsSearchable}" MaxChips="2" />''') \
      + filter_theme("HelixDateFilter", 250, '''<h:HelixDateRangePicker SelectedRange="{Binding Range, RelativeSource={RelativeSource TemplatedParent}, Mode=TwoWay}"
                                      Placeholder="{TemplateBinding Label}" MaxDays="{TemplateBinding MaxDays}" />''') \
      + filter_theme("HelixToggleFilter", None, '''<h:HelixSwitch Content="{TemplateBinding Label}" VerticalAlignment="Center"
                             IsChecked="{Binding IsOn, RelativeSource={RelativeSource TemplatedParent}, Mode=TwoWay}" />''',
                     margin="4,0,14,8", extra='\n        <Setter Property="VerticalAlignment" Value="Center" />') + '''
    <!-- ===================================================================== Color picker -->

''' + picker("HelixColorPicker", '''                                <Border Name="PART_Panel" />''', COLOR_VALUE) + '''
    <!-- ===================================================================== File drop -->

    <ControlTheme x:Key="{x:Type h:HelixFileDrop}" TargetType="h:HelixFileDrop">
        <Setter Property="Accent" Value="{DynamicResource Helix.Primary}" />
        <Setter Property="FontFamily" Value="{DynamicResource Helix.FontFamily}" />
        <Setter Property="FontSize" Value="{DynamicResource Helix.FontSize}" />
        <Setter Property="Foreground" Value="{DynamicResource Helix.Text}" />
        <Setter Property="Background" Value="{DynamicResource Helix.Surface}" />
        <Setter Property="CornerRadius" Value="14" />
        <Setter Property="Template">
            <ControlTemplate>
                <StackPanel>
                    <Panel Name="Zone">
                        <Border CornerRadius="{TemplateBinding CornerRadius}" Background="{TemplateBinding Background}" />
                        <Border Name="Tint" CornerRadius="{TemplateBinding CornerRadius}" Background="{TemplateBinding Accent}" Opacity="0" />
                        <Rectangle Name="Dash" RadiusX="14" RadiusY="14" StrokeDashArray="5,4" StrokeThickness="1.5" Stroke="{DynamicResource Helix.BorderStrong}" />
                        <StackPanel Name="Big" Margin="24,28" HorizontalAlignment="Center">
                            <Panel Width="44" Height="44" HorizontalAlignment="Center">
                                <Ellipse Fill="{TemplateBinding Accent}" Opacity="0.1" />
                                <h:HelixIcon Icon="upload" Size="20" Foreground="{TemplateBinding Accent}" HorizontalAlignment="Center" VerticalAlignment="Center" />
                            </Panel>
                            <TextBlock Text="{TemplateBinding Title}" FontWeight="SemiBold" Margin="0,12,0,0" HorizontalAlignment="Center" />
                            <TextBlock Text="{TemplateBinding AutoHint}" FontSize="12.5" Margin="0,3,0,0" HorizontalAlignment="Center" Foreground="{DynamicResource Helix.TextMuted}"
                                       IsVisible="{TemplateBinding AutoHint, Converter={x:Static StringConverters.IsNotNullOrEmpty}}" />
                            <h:HelixButton Name="PART_Browse" Content="{TemplateBinding BrowseText}" Variant="Outline" Tone="Neutral" Size="Sm" Margin="0,14,0,0" HorizontalAlignment="Center" />
                        </StackPanel>
                    </Panel>
                    <TextBlock Text="{TemplateBinding Error}" Margin="0,8,0,0" FontSize="12.5" TextWrapping="Wrap" Foreground="{DynamicResource Helix.Danger}"
                               IsVisible="{TemplateBinding Error, Converter={x:Static StringConverters.IsNotNullOrEmpty}}" />
                    <ItemsControl Name="List" ItemsSource="{Binding Files, RelativeSource={RelativeSource TemplatedParent}}" Margin="0,10,0,0" IsVisible="{TemplateBinding HasFiles}">
                        <ItemsControl.ItemTemplate>
                            <DataTemplate DataType="h:HelixFile">
                                <Border Margin="0,0,0,6" Padding="10,8" CornerRadius="10" Background="{DynamicResource Helix.Surface}" BorderBrush="{DynamicResource Helix.Border}" BorderThickness="1">
                                    <DockPanel>
                                        <Border DockPanel.Dock="Left" Width="34" Height="34" CornerRadius="8" Background="{DynamicResource Helix.Surface2}" Margin="0,0,10,0">
                                            <TextBlock Text="{Binding Extension}" FontSize="10" FontWeight="Bold" Foreground="{DynamicResource Helix.TextMuted}" HorizontalAlignment="Center" VerticalAlignment="Center" />
                                        </Border>
                                        <Button DockPanel.Dock="Right" Theme="{StaticResource HelixIconButton}" Command="{Binding RemoveCommand}" VerticalAlignment="Center">
                                            <h:HelixIcon Icon="x" Size="14" />
                                        </Button>
                                        <StackPanel VerticalAlignment="Center">
                                            <TextBlock Text="{Binding Name}" FontWeight="Medium" TextTrimming="CharacterEllipsis" Foreground="{DynamicResource Helix.Text}" />
                                            <TextBlock Text="{Binding SizeText}" FontSize="12" Foreground="{DynamicResource Helix.TextMuted}" />
                                        </StackPanel>
                                    </DockPanel>
                                </Border>
                            </DataTemplate>
                        </ItemsControl.ItemTemplate>
                    </ItemsControl>
                </StackPanel>
            </ControlTemplate>
        </Setter>
''' + tones + '''
        <Style Selector="^[IsCompact=True] /template/ StackPanel#Big"><Setter Property="Margin" Value="16,10" /></Style>
        <Style Selector="^[ShowList=False] /template/ ItemsControl#List"><Setter Property="IsVisible" Value="False" /></Style>
        <Style Selector="^:pointerover /template/ Rectangle#Dash"><Setter Property="Stroke" Value="{DynamicResource Helix.TextSubtle}" /></Style>
        <Style Selector="^:dragover /template/ Rectangle#Dash"><Setter Property="Stroke" Value="{Binding Accent, RelativeSource={RelativeSource TemplatedParent}}" /></Style>
        <Style Selector="^:dragover /template/ Border#Tint"><Setter Property="Opacity" Value="0.07" /></Style>
        <Style Selector="^:disabled"><Setter Property="Opacity" Value="0.5" /></Style>
    </ControlTheme>
'''
