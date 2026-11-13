"""Templates of the fields and pickers, appended to Controls.axaml by gen_avalonia_theme.py."""

def label_row():
    return '''                    <StackPanel Orientation="Horizontal" Spacing="3" IsVisible="{TemplateBinding Label, Converter={x:Static StringConverters.IsNotNullOrEmpty}}">
                        <TextBlock Text="{TemplateBinding Label}" FontSize="13" FontWeight="SemiBold" />
                        <TextBlock Text="*" FontSize="13" FontWeight="SemiBold" Foreground="{DynamicResource Helix.Danger}" IsVisible="{TemplateBinding IsRequired}" />
                    </StackPanel>'''

def messages():
    return '''                    <TextBlock Name="Hint" Text="{TemplateBinding Hint}" FontSize="12.5" Foreground="{DynamicResource Helix.TextMuted}" TextWrapping="Wrap"
                               IsVisible="{TemplateBinding Hint, Converter={x:Static StringConverters.IsNotNullOrEmpty}}" />
                    <TextBlock Name="Error" Text="{TemplateBinding Error}" FontSize="12.5" Foreground="{DynamicResource Helix.Danger}" TextWrapping="Wrap" IsVisible="False" />'''

def frame_states(extra_focus=":focus-within"):
    return f'''        <Style Selector="^:pointerover /template/ Border#Frame"><Setter Property="BorderBrush" Value="{{DynamicResource Helix.TextSubtle}}" /></Style>
        <Style Selector="^{extra_focus} /template/ Border#Frame"><Setter Property="BorderBrush" Value="{{DynamicResource Helix.Primary}}" /></Style>
        <Style Selector="^{extra_focus} /template/ Border#Halo"><Setter Property="Opacity" Value="0.18" /></Style>
        <Style Selector="^:error /template/ Border#Frame"><Setter Property="BorderBrush" Value="{{DynamicResource Helix.Danger}}" /></Style>
        <Style Selector="^:error /template/ Border#Halo"><Setter Property="BorderBrush" Value="{{DynamicResource Helix.Danger}}" /></Style>
        <Style Selector="^:error /template/ TextBlock#Error"><Setter Property="IsVisible" Value="True" /></Style>
        <Style Selector="^:error /template/ TextBlock#Hint"><Setter Property="IsVisible" Value="False" /></Style>
        <Style Selector="^:disabled"><Setter Property="Opacity" Value="0.55" /></Style>'''

FIELD_SETTERS = '''        <Setter Property="FontFamily" Value="{DynamicResource Helix.FontFamily}" />
        <Setter Property="FontSize" Value="{DynamicResource Helix.FontSize}" />
        <Setter Property="Foreground" Value="{DynamicResource Helix.Text}" />
        <Setter Property="Background" Value="{DynamicResource Helix.Surface}" />
        <Setter Property="BorderBrush" Value="{DynamicResource Helix.BorderStrong}" />
        <Setter Property="BorderThickness" Value="1" />
        <Setter Property="CornerRadius" Value="{DynamicResource Helix.Radius}" />'''

POPUP_BORDER = 'Background="{DynamicResource Helix.Surface}" BorderBrush="{DynamicResource Helix.Border}" BorderThickness="1" CornerRadius="12" BoxShadow="0 12 32 0 #26000000" Margin="0,0,12,12"'

def picker(type_name, popup, value_part=None):
    """A field that opens a panel: icon, text (or custom value part), clear and chevron; the popup content below."""
    value_part = value_part or '''<Panel>
                                        <TextBlock Text="{TemplateBinding Placeholder}" Foreground="{DynamicResource Helix.TextSubtle}" VerticalAlignment="Center"
                                                   IsVisible="{TemplateBinding HasValue, Converter={x:Static BoolConverters.Not}}" />
                                        <TextBlock Text="{TemplateBinding DisplayText}" VerticalAlignment="Center" TextTrimming="CharacterEllipsis" />
                                    </Panel>'''
    return f'''    <ControlTheme x:Key="{{x:Type h:{type_name}}}" TargetType="h:{type_name}">
{FIELD_SETTERS}
        <Setter Property="Template">
            <ControlTemplate>
                <StackPanel Spacing="6">
{label_row()}
                    <Panel>
                        <Border Name="Halo" Margin="-3" CornerRadius="13" BorderThickness="3" BorderBrush="{{DynamicResource Helix.Primary}}" Opacity="0" />
                        <Border Name="Frame" MinHeight="{{DynamicResource Helix.ControlHeight}}" Background="{{TemplateBinding Background}}" BorderBrush="{{TemplateBinding BorderBrush}}"
                                BorderThickness="{{TemplateBinding BorderThickness}}" CornerRadius="{{TemplateBinding CornerRadius}}">
                            <Panel>
                                <ToggleButton Name="Toggle" Theme="{{StaticResource HelixBareToggle}}" IsChecked="{{TemplateBinding IsDropDownOpen, Mode=TwoWay}}"
                                              HorizontalAlignment="Stretch" VerticalAlignment="Stretch" HorizontalContentAlignment="Stretch" Padding="12,0">
                                    <DockPanel>
                                        <h:HelixIcon DockPanel.Dock="Left" Icon="{{TemplateBinding Icon}}" Size="16" Foreground="{{DynamicResource Helix.TextSubtle}}"
                                                     VerticalAlignment="Center" Margin="0,0,8,0" IsVisible="{{TemplateBinding Icon, Converter={{x:Static ObjectConverters.IsNotNull}}}}" />
                                        <h:HelixIcon DockPanel.Dock="Right" Icon="chevron-down" Size="15" Foreground="{{DynamicResource Helix.TextSubtle}}" VerticalAlignment="Center" Margin="8,0,0,0" />
                                        <Border DockPanel.Dock="Right" Width="26" Name="ClearSpace" IsVisible="False" />
                                        {value_part}
                                    </DockPanel>
                                </ToggleButton>
                                <Button Name="PART_Clear" Theme="{{StaticResource HelixIconButton}}" Width="22" Height="22" HorizontalAlignment="Right" VerticalAlignment="Center"
                                        Margin="0,0,32,0" IsVisible="False">
                                    <h:HelixIcon Icon="x" Size="13" />
                                </Button>
                            </Panel>
                        </Border>
                        <Popup Name="PART_Popup" IsOpen="{{TemplateBinding IsDropDownOpen, Mode=TwoWay}}" PlacementTarget="{{Binding #Frame}}" Placement="BottomEdgeAlignedLeft"
                               VerticalOffset="6" IsLightDismissEnabled="True">
                            <Border {POPUP_BORDER} Padding="12">
{popup}
                            </Border>
                        </Popup>
                    </Panel>
{messages()}
                </StackPanel>
            </ControlTemplate>
        </Setter>
{frame_states(":focus-within")}
        <Style Selector="^[IsDropDownOpen=True] /template/ Border#Frame"><Setter Property="BorderBrush" Value="{{DynamicResource Helix.Primary}}" /></Style>
        <Style Selector="^[IsDropDownOpen=True] /template/ Border#Halo"><Setter Property="Opacity" Value="0.18" /></Style>
        <Style Selector="^:clearable /template/ Button#PART_Clear"><Setter Property="IsVisible" Value="True" /></Style>
        <Style Selector="^:clearable /template/ Border#ClearSpace"><Setter Property="IsVisible" Value="True" /></Style>
    </ControlTheme>
'''

CALENDAR = '''<h:HelixCalendar Name="PART_Calendar" />'''

def build():
    out = []
    out.append('''
    <!-- ===================================================================== Fields and pickers -->

    <!-- The − / + buttons of the number field. -->
    <ControlTheme x:Key="HelixStepper" TargetType="Button">
        <Setter Property="Width" Value="36" />
        <Setter Property="Background" Value="Transparent" />
        <Setter Property="Foreground" Value="{DynamicResource Helix.TextMuted}" />
        <Setter Property="Cursor" Value="Hand" />
        <Setter Property="Template">
            <ControlTemplate>
                <Border Background="{TemplateBinding Background}">
                    <ContentPresenter Content="{TemplateBinding Content}" HorizontalAlignment="Center" VerticalAlignment="Center" />
                </Border>
            </ControlTemplate>
        </Setter>
        <Style Selector="^:pointerover">
            <Setter Property="Background" Value="{DynamicResource Helix.Surface2}" />
            <Setter Property="Foreground" Value="{DynamicResource Helix.Text}" />
        </Style>
    </ControlTheme>

    <!-- A toggle without its own look: the clickable part of a picker field. -->
    <ControlTheme x:Key="HelixBareToggle" TargetType="ToggleButton">
        <Setter Property="Background" Value="Transparent" />
        <Setter Property="Cursor" Value="Hand" />
        <Setter Property="Template">
            <ControlTemplate>
                <Border Background="{TemplateBinding Background}" Padding="{TemplateBinding Padding}">
                    <ContentPresenter Content="{TemplateBinding Content}" HorizontalAlignment="{TemplateBinding HorizontalContentAlignment}" VerticalAlignment="Stretch" />
                </Border>
            </ControlTemplate>
        </Setter>
    </ControlTheme>

    <ControlTheme x:Key="HelixCalendarDay" TargetType="Button">
        <Setter Property="Width" Value="36" />
        <Setter Property="Height" Value="34" />
        <Setter Property="Padding" Value="0" />
        <Setter Property="Margin" Value="1" />
        <Setter Property="FontSize" Value="13" />
        <Setter Property="Background" Value="Transparent" />
        <Setter Property="Foreground" Value="{DynamicResource Helix.Text}" />
        <Setter Property="Cursor" Value="Hand" />
        <Setter Property="Template">
            <ControlTemplate>
                <Border Name="Bg" Background="{TemplateBinding Background}" CornerRadius="8" BorderBrush="{TemplateBinding BorderBrush}" BorderThickness="{TemplateBinding BorderThickness}">
                    <ContentPresenter Content="{TemplateBinding Content}" HorizontalAlignment="Center" VerticalAlignment="Center" />
                </Border>
            </ControlTemplate>
        </Setter>
        <Style Selector="^:pointerover"><Setter Property="Background" Value="{DynamicResource Helix.Surface2}" /></Style>
        <Style Selector="^.outside"><Setter Property="Foreground" Value="{DynamicResource Helix.TextSubtle}" /></Style>
        <Style Selector="^.today">
            <Setter Property="BorderBrush" Value="{DynamicResource Helix.Primary}" />
            <Setter Property="BorderThickness" Value="1" />
            <Setter Property="FontWeight" Value="SemiBold" />
        </Style>
        <Style Selector="^.in-range"><Setter Property="Background" Value="{DynamicResource Helix.PrimarySoft}" /></Style>
        <Style Selector="^.selected">
            <Setter Property="Background" Value="{DynamicResource Helix.Primary}" />
            <Setter Property="Foreground" Value="{DynamicResource Helix.OnPrimary}" />
            <Setter Property="FontWeight" Value="SemiBold" />
        </Style>
        <Style Selector="^:disabled">
            <Setter Property="Foreground" Value="{DynamicResource Helix.TextSubtle}" />
            <Setter Property="Opacity" Value="0.45" />
        </Style>
    </ControlTheme>

    <ControlTheme x:Key="{x:Type h:HelixCalendar}" TargetType="h:HelixCalendar">
        <Setter Property="FontFamily" Value="{DynamicResource Helix.FontFamily}" />
        <Setter Property="Foreground" Value="{DynamicResource Helix.Text}" />
        <Setter Property="Focusable" Value="True" />
        <Setter Property="Template">
            <ControlTemplate>
                <StackPanel Spacing="6">
                    <DockPanel>
                        <Button Name="PART_Previous" DockPanel.Dock="Left" Theme="{StaticResource HelixIconButton}" Width="30" Height="30"><h:HelixIcon Icon="chevron-left" Size="16" /></Button>
                        <Button Name="PART_Next" DockPanel.Dock="Right" Theme="{StaticResource HelixIconButton}" Width="30" Height="30"><h:HelixIcon Icon="chevron-right" Size="16" /></Button>
                        <TextBlock Text="{TemplateBinding MonthText}" FontWeight="SemiBold" FontSize="14" HorizontalAlignment="Center" VerticalAlignment="Center" />
                    </DockPanel>
                    <UniformGrid Name="PART_Days" Columns="7" />
                </StackPanel>
            </ControlTemplate>
        </Setter>
    </ControlTheme>
''')
    out.append(picker("HelixDatePicker", f'''                                <StackPanel Spacing="8">
                                    {CALENDAR}
                                    <h:HelixButton Name="PART_Today" Content="{{x:Static h:HelixPickerBase.TodayText}}" Variant="Ghost" Size="Sm" HorizontalAlignment="Left" />
                                </StackPanel>'''))
    out.append(picker("HelixDateTimePicker", f'''                                <StackPanel Spacing="8">
                                    <StackPanel Orientation="Horizontal" Spacing="12">
                                        {CALENDAR}
                                        <Border Width="1" Background="{{DynamicResource Helix.Border}}" />
                                        <ListBox Name="PART_Times" Width="96" Height="272" Background="Transparent">
                                            <ListBox.Styles>
                                                <Style Selector="ListBoxItem">
                                                    <Setter Property="Padding" Value="10,6" />
                                                    <Setter Property="CornerRadius" Value="7" />
                                                    <Setter Property="IsEnabled" Value="{{ReflectionBinding IsEnabled}}" />
                                                </Style>
                                                <Style Selector="ListBoxItem:selected /template/ ContentPresenter#PART_ContentPresenter">
                                                    <Setter Property="Background" Value="{{DynamicResource Helix.Primary}}" />
                                                    <Setter Property="Foreground" Value="{{DynamicResource Helix.OnPrimary}}" />
                                                </Style>
                                            </ListBox.Styles>
                                            <ListBox.ItemTemplate>
                                                <DataTemplate x:DataType="h:HelixTimeSlot">
                                                    <TextBlock Text="{{Binding Text}}" FontSize="13" />
                                                </DataTemplate>
                                            </ListBox.ItemTemplate>
                                        </ListBox>
                                    </StackPanel>
                                    <h:HelixButton Name="PART_Now" Content="{{x:Static h:HelixPickerBase.NowText}}" Variant="Ghost" Size="Sm" HorizontalAlignment="Left" />
                                </StackPanel>'''))
    out.append(picker("HelixDateRangePicker", f'''                                <StackPanel Orientation="Horizontal" Spacing="12">
                                    <StackPanel Name="PART_Presets" Width="150" Spacing="2" IsVisible="{{TemplateBinding ShowPresets}}" />
                                    <Border Width="1" Background="{{DynamicResource Helix.Border}}" IsVisible="{{TemplateBinding ShowPresets}}" />
                                    <StackPanel Spacing="6">
                                        {CALENDAR}
                                        <TextBlock Text="{{x:Static h:HelixDateRangePicker.PickEndText}}" FontSize="12.5" Foreground="{{DynamicResource Helix.Primary}}"
                                                   HorizontalAlignment="Center" IsVisible="{{TemplateBinding IsPickingEnd}}" />
                                    </StackPanel>
                                </StackPanel>'''))
    out.append(picker("HelixMultiSelect", f'''                                <StackPanel Spacing="8" Width="{{Binding #Frame.Bounds.Width}}" MinWidth="220">
                                    <Border BorderBrush="{{DynamicResource Helix.BorderStrong}}" BorderThickness="1" CornerRadius="8" Padding="10,0" Height="34"
                                            IsVisible="{{TemplateBinding IsSearchable}}">
                                        <DockPanel>
                                            <h:HelixIcon DockPanel.Dock="Left" Icon="search" Size="14" Foreground="{{DynamicResource Helix.TextSubtle}}" Margin="0,0,8,0" VerticalAlignment="Center" />
                                            <TextBox Name="PART_Search" Theme="{{StaticResource HelixInput}}" Text="{{TemplateBinding SearchText, Mode=TwoWay}}"
                                                     PlaceholderText="{{x:Static h:HelixMultiSelect.SearchPlaceholderText}}" />
                                        </DockPanel>
                                    </Border>
                                    <ScrollViewer MaxHeight="260">
                                        <StackPanel Name="PART_List" />
                                    </ScrollViewer>
                                    <DockPanel IsVisible="{{TemplateBinding ShowActions}}">
                                        <h:HelixButton Name="PART_ClearAll" DockPanel.Dock="Right" Content="{{x:Static h:HelixMultiSelect.ClearText}}" Variant="Ghost" Tone="Neutral" Size="Sm" />
                                        <h:HelixButton Name="PART_SelectAll" Content="{{x:Static h:HelixMultiSelect.SelectAllText}}" Variant="Ghost" Size="Sm" HorizontalAlignment="Left" />
                                    </DockPanel>
                                </StackPanel>''',
        value_part='''<Panel>
                                        <TextBlock Text="{TemplateBinding Placeholder}" Foreground="{DynamicResource Helix.TextSubtle}" VerticalAlignment="Center"
                                                   IsVisible="{TemplateBinding HasSelection, Converter={x:Static BoolConverters.Not}}" />
                                        <WrapPanel VerticalAlignment="Center" Margin="0,4" ItemSpacing="4" LineSpacing="4">
                                            <ItemsControl ItemsSource="{TemplateBinding Chips}">
                                                <ItemsControl.ItemsPanel><ItemsPanelTemplate><WrapPanel ItemSpacing="4" LineSpacing="4" /></ItemsPanelTemplate></ItemsControl.ItemsPanel>
                                                <ItemsControl.ItemTemplate>
                                                    <DataTemplate x:DataType="h:HelixChip">
                                                        <Border Background="{DynamicResource Helix.PrimarySoft}" CornerRadius="6" Padding="8,2,4,2">
                                                            <StackPanel Orientation="Horizontal" Spacing="4">
                                                                <TextBlock Text="{Binding Text}" FontSize="12.5" FontWeight="Medium" Foreground="{DynamicResource Helix.Primary}" VerticalAlignment="Center" />
                                                                <Button Theme="{StaticResource HelixIconButton}" Width="16" Height="16" Command="{Binding RemoveCommand}">
                                                                    <h:HelixIcon Icon="x" Size="11" />
                                                                </Button>
                                                            </StackPanel>
                                                        </Border>
                                                    </DataTemplate>
                                                </ItemsControl.ItemTemplate>
                                            </ItemsControl>
                                            <Border Background="{DynamicResource Helix.Surface2}" CornerRadius="6" Padding="7,2" VerticalAlignment="Center"
                                                    IsVisible="{TemplateBinding MoreText, Converter={x:Static StringConverters.IsNotNullOrEmpty}}">
                                                <TextBlock Text="{TemplateBinding MoreText}" FontSize="12.5" FontWeight="SemiBold" Foreground="{DynamicResource Helix.TextMuted}" />
                                            </Border>
                                        </WrapPanel>
                                    </Panel>'''))

    # Number field
    out.append(f'''    <ControlTheme x:Key="{{x:Type h:HelixNumberField}}" TargetType="h:HelixNumberField">
{FIELD_SETTERS}
        <Setter Property="Template">
            <ControlTemplate>
                <StackPanel Spacing="6">
{label_row()}
                    <Panel>
                        <Border Name="Halo" Margin="-3" CornerRadius="13" BorderThickness="3" BorderBrush="{{DynamicResource Helix.Primary}}" Opacity="0" />
                        <Border Name="Frame" MinHeight="{{DynamicResource Helix.ControlHeight}}" Background="{{TemplateBinding Background}}" BorderBrush="{{TemplateBinding BorderBrush}}"
                                BorderThickness="{{TemplateBinding BorderThickness}}" CornerRadius="{{TemplateBinding CornerRadius}}" ClipToBounds="True">
                            <DockPanel>
                                <Button Name="PART_Down" DockPanel.Dock="Left" Theme="{{StaticResource HelixStepper}}" IsVisible="{{TemplateBinding ShowSteppers}}"><h:HelixIcon Icon="minus" Size="14" /></Button>
                                <Button Name="PART_Up" DockPanel.Dock="Right" Theme="{{StaticResource HelixStepper}}" IsVisible="{{TemplateBinding ShowSteppers}}"><h:HelixIcon Icon="plus" Size="14" /></Button>
                                <TextBlock DockPanel.Dock="Right" Text="{{TemplateBinding Unit}}" Foreground="{{DynamicResource Helix.TextMuted}}" VerticalAlignment="Center" Margin="6,0,10,0"
                                           IsVisible="{{TemplateBinding Unit, Converter={{x:Static StringConverters.IsNotNullOrEmpty}}}}" />
                                <TextBox Name="PART_Input" Theme="{{StaticResource HelixInput}}" Text="{{TemplateBinding Text, Mode=TwoWay}}" IsReadOnly="{{TemplateBinding IsReadOnly}}"
                                         PlaceholderText="{{TemplateBinding Placeholder}}" TextAlignment="Center" Margin="8,0" FontSize="{{TemplateBinding FontSize}}"
                                         FontFamily="{{TemplateBinding FontFamily}}" />
                            </DockPanel>
                        </Border>
                    </Panel>
{messages()}
                </StackPanel>
            </ControlTemplate>
        </Setter>
{frame_states()}
        <Style Selector="^:good /template/ TextBox#PART_Input"><Setter Property="Foreground" Value="{{DynamicResource Helix.Success}}" /></Style>
        <Style Selector="^:bad /template/ TextBox#PART_Input"><Setter Property="Foreground" Value="{{DynamicResource Helix.Danger}}" /></Style>
    </ControlTheme>
''')

    # Select
    out.append(f'''    <ControlTheme x:Key="HelixSelectItem" TargetType="ComboBoxItem">
        <Setter Property="Padding" Value="10,7" />
        <Setter Property="Margin" Value="0,1" />
        <Setter Property="Background" Value="Transparent" />
        <Setter Property="Foreground" Value="{{DynamicResource Helix.Text}}" />
        <Setter Property="Cursor" Value="Hand" />
        <Setter Property="Template">
            <ControlTemplate>
                <Border Background="{{TemplateBinding Background}}" CornerRadius="7" Padding="{{TemplateBinding Padding}}">
                    <ContentPresenter Name="PART_ContentPresenter" Content="{{TemplateBinding Content}}" ContentTemplate="{{TemplateBinding ContentTemplate}}" />
                </Border>
            </ControlTemplate>
        </Setter>
        <Style Selector="^:pointerover"><Setter Property="Background" Value="{{DynamicResource Helix.Surface2}}" /></Style>
        <Style Selector="^:selected">
            <Setter Property="Background" Value="{{DynamicResource Helix.PrimarySoft}}" />
            <Setter Property="Foreground" Value="{{DynamicResource Helix.Primary}}" />
            <Setter Property="FontWeight" Value="SemiBold" />
        </Style>
    </ControlTheme>

    <ControlTheme x:Key="{{x:Type h:HelixSelect}}" TargetType="h:HelixSelect">
{FIELD_SETTERS}
        <Setter Property="PlaceholderText" Value="{{x:Static h:HelixSelect.SelectPlaceholderText}}" />
        <Setter Property="ItemContainerTheme" Value="{{StaticResource HelixSelectItem}}" />
        <Setter Property="MaxDropDownHeight" Value="300" />
        <Setter Property="Cursor" Value="Hand" />
        <Setter Property="HorizontalAlignment" Value="Stretch" />
        <Setter Property="ItemsPanel">
            <ItemsPanelTemplate><StackPanel /></ItemsPanelTemplate>
        </Setter>
        <Setter Property="Template">
            <ControlTemplate>
                <StackPanel Spacing="6">
{label_row()}
                    <Panel>
                        <Border Name="Halo" Margin="-3" CornerRadius="13" BorderThickness="3" BorderBrush="{{DynamicResource Helix.Primary}}" Opacity="0" />
                        <Border Name="Frame" MinHeight="{{DynamicResource Helix.ControlHeight}}" Background="{{TemplateBinding Background}}" BorderBrush="{{TemplateBinding BorderBrush}}"
                                BorderThickness="{{TemplateBinding BorderThickness}}" CornerRadius="{{TemplateBinding CornerRadius}}" Padding="12,0">
                            <DockPanel>
                                <h:HelixIcon DockPanel.Dock="Left" Icon="{{TemplateBinding Icon}}" Size="16" Foreground="{{DynamicResource Helix.TextSubtle}}"
                                             VerticalAlignment="Center" Margin="0,0,8,0" IsVisible="{{TemplateBinding Icon, Converter={{x:Static ObjectConverters.IsNotNull}}}}" />
                                <h:HelixIcon DockPanel.Dock="Right" Icon="chevron-down" Size="15" Foreground="{{DynamicResource Helix.TextSubtle}}" VerticalAlignment="Center" Margin="8,0,0,0" />
                                <Panel VerticalAlignment="Center">
                                    <TextBlock Text="{{TemplateBinding PlaceholderText}}" Foreground="{{DynamicResource Helix.TextSubtle}}"
                                               IsVisible="{{TemplateBinding SelectionBoxItem, Converter={{x:Static ObjectConverters.IsNull}}}}" />
                                    <ContentControl Content="{{TemplateBinding SelectionBoxItem}}" ContentTemplate="{{TemplateBinding SelectionBoxItemTemplate}}" />
                                </Panel>
                            </DockPanel>
                        </Border>
                        <Popup Name="PART_Popup" IsOpen="{{TemplateBinding IsDropDownOpen, Mode=TwoWay}}" PlacementTarget="{{Binding #Frame}}" Placement="BottomEdgeAlignedLeft"
                               VerticalOffset="6" IsLightDismissEnabled="True" MinWidth="{{Binding #Frame.Bounds.Width}}">
                            <Border {POPUP_BORDER} Padding="6">
                                <DockPanel MaxHeight="{{TemplateBinding MaxDropDownHeight}}">
                                    <Border DockPanel.Dock="Top" BorderBrush="{{DynamicResource Helix.BorderStrong}}" BorderThickness="1" CornerRadius="8" Padding="10,0" Height="34"
                                            Margin="0,0,0,6" IsVisible="{{TemplateBinding IsSearchable}}">
                                        <DockPanel>
                                            <h:HelixIcon DockPanel.Dock="Left" Icon="search" Size="14" Foreground="{{DynamicResource Helix.TextSubtle}}" Margin="0,0,8,0" VerticalAlignment="Center" />
                                            <TextBox Name="PART_Search" Theme="{{StaticResource HelixInput}}" Text="{{TemplateBinding SearchText, Mode=TwoWay}}"
                                                     PlaceholderText="{{x:Static h:HelixSelect.SearchPlaceholderText}}" />
                                        </DockPanel>
                                    </Border>
                                    <ScrollViewer>
                                        <ItemsPresenter Name="PART_ItemsPresenter" ItemsPanel="{{TemplateBinding ItemsPanel}}" />
                                    </ScrollViewer>
                                </DockPanel>
                            </Border>
                        </Popup>
                    </Panel>
{messages()}
                </StackPanel>
            </ControlTemplate>
        </Setter>
{frame_states(":focus")}
        <Style Selector="^[IsDropDownOpen=True] /template/ Border#Frame"><Setter Property="BorderBrush" Value="{{DynamicResource Helix.Primary}}" /></Style>
        <Style Selector="^[IsDropDownOpen=True] /template/ Border#Halo"><Setter Property="Opacity" Value="0.18" /></Style>
    </ControlTheme>
''')

    # Radio group + items
    out.append('''    <ControlTheme x:Key="HelixRadioItem" TargetType="ListBoxItem">
        <Setter Property="Background" Value="Transparent" />
        <Setter Property="Foreground" Value="{DynamicResource Helix.Text}" />
        <Setter Property="Cursor" Value="Hand" />
        <Setter Property="Template">
            <ControlTemplate>
                <DockPanel Background="{TemplateBinding Background}">
                    <Panel DockPanel.Dock="Left" Width="18" Height="18" VerticalAlignment="Top" Margin="0,1,10,0">
                        <Ellipse Name="Ring" Stroke="{DynamicResource Helix.BorderStrong}" StrokeThickness="1.5" Fill="{DynamicResource Helix.Surface}" />
                        <Ellipse Name="Dot" Width="8" Height="8" Fill="{DynamicResource Helix.OnPrimary}" Opacity="0" />
                    </Panel>
                    <ContentPresenter Name="PART_ContentPresenter" Content="{TemplateBinding Content}" ContentTemplate="{TemplateBinding ContentTemplate}" VerticalAlignment="Center" />
                </DockPanel>
            </ControlTemplate>
        </Setter>
        <Style Selector="^:pointerover /template/ Ellipse#Ring"><Setter Property="Stroke" Value="{DynamicResource Helix.Primary}" /></Style>
        <Style Selector="^:selected /template/ Ellipse#Ring">
            <Setter Property="Stroke" Value="{DynamicResource Helix.Primary}" />
            <Setter Property="Fill" Value="{DynamicResource Helix.Primary}" />
        </Style>
        <Style Selector="^:selected /template/ Ellipse#Dot"><Setter Property="Opacity" Value="1" /></Style>
        <Style Selector="^:disabled"><Setter Property="Opacity" Value="0.5" /></Style>
    </ControlTheme>

    <ControlTheme x:Key="HelixRadioCard" TargetType="ListBoxItem">
        <Setter Property="Background" Value="{DynamicResource Helix.Surface}" />
        <Setter Property="BorderBrush" Value="{DynamicResource Helix.Border}" />
        <Setter Property="Foreground" Value="{DynamicResource Helix.Text}" />
        <Setter Property="Cursor" Value="Hand" />
        <Setter Property="Template">
            <ControlTemplate>
                <Border Background="{TemplateBinding Background}" BorderBrush="{TemplateBinding BorderBrush}" BorderThickness="1.5" CornerRadius="12" Padding="14,12">
                    <DockPanel>
                        <Panel DockPanel.Dock="Right" Width="18" Height="18" VerticalAlignment="Top" Margin="12,0,0,0">
                            <Ellipse Name="Ring" Stroke="{DynamicResource Helix.BorderStrong}" StrokeThickness="1.5" />
                            <h:HelixIcon Name="Check" Icon="check" Size="12" StrokeWidth="3" Foreground="{DynamicResource Helix.OnPrimary}" HorizontalAlignment="Center" VerticalAlignment="Center" Opacity="0" />
                        </Panel>
                        <ContentPresenter Name="PART_ContentPresenter" Content="{TemplateBinding Content}" ContentTemplate="{TemplateBinding ContentTemplate}" />
                    </DockPanel>
                </Border>
            </ControlTemplate>
        </Setter>
        <Style Selector="^:pointerover"><Setter Property="BorderBrush" Value="{DynamicResource Helix.BorderStrong}" /></Style>
        <Style Selector="^:selected">
            <Setter Property="BorderBrush" Value="{DynamicResource Helix.Primary}" />
            <Setter Property="Background" Value="{DynamicResource Helix.PrimarySoft}" />
        </Style>
        <Style Selector="^:selected /template/ Ellipse#Ring">
            <Setter Property="Fill" Value="{DynamicResource Helix.Primary}" />
            <Setter Property="Stroke" Value="{DynamicResource Helix.Primary}" />
        </Style>
        <Style Selector="^:selected /template/ h|HelixIcon#Check"><Setter Property="Opacity" Value="1" /></Style>
        <Style Selector="^:disabled"><Setter Property="Opacity" Value="0.5" /></Style>
    </ControlTheme>

    <ControlTheme x:Key="HelixRadioSegment" TargetType="ListBoxItem">
        <Setter Property="Background" Value="Transparent" />
        <Setter Property="Foreground" Value="{DynamicResource Helix.TextMuted}" />
        <Setter Property="FontWeight" Value="Medium" />
        <Setter Property="Cursor" Value="Hand" />
        <Setter Property="Template">
            <ControlTemplate>
                <Border Name="Bg" Background="{TemplateBinding Background}" CornerRadius="7" Padding="14,6">
                    <ContentPresenter Name="PART_ContentPresenter" Content="{TemplateBinding Content}" ContentTemplate="{TemplateBinding ContentTemplate}" HorizontalAlignment="Center" />
                </Border>
            </ControlTemplate>
        </Setter>
        <Style Selector="^:pointerover"><Setter Property="Foreground" Value="{DynamicResource Helix.Text}" /></Style>
        <Style Selector="^:selected">
            <Setter Property="Background" Value="{DynamicResource Helix.Surface}" />
            <Setter Property="Foreground" Value="{DynamicResource Helix.Text}" />
        </Style>
        <Style Selector="^:selected /template/ Border#Bg"><Setter Property="BoxShadow" Value="0 1 3 0 #24000000" /></Style>
    </ControlTheme>

    <ControlTheme x:Key="{x:Type h:HelixRadioGroup}" TargetType="h:HelixRadioGroup">
        <Setter Property="FontFamily" Value="{DynamicResource Helix.FontFamily}" />
        <Setter Property="FontSize" Value="{DynamicResource Helix.FontSize}" />
        <Setter Property="Foreground" Value="{DynamicResource Helix.Text}" />
        <Setter Property="Background" Value="Transparent" />
        <Setter Property="SelectionMode" Value="Single,AlwaysSelected" />
        <Setter Property="ItemsPanel">
            <ItemsPanelTemplate><StackPanel Spacing="10" /></ItemsPanelTemplate>
        </Setter>
        <Setter Property="Template">
            <ControlTemplate>
                <StackPanel Spacing="8">
                    <TextBlock Text="{TemplateBinding Label}" FontSize="13" FontWeight="SemiBold"
                               IsVisible="{TemplateBinding Label, Converter={x:Static StringConverters.IsNotNullOrEmpty}}" />
                    <Border Name="Track" HorizontalAlignment="Left">
                        <ItemsPresenter Name="PART_ItemsPresenter" ItemsPanel="{TemplateBinding ItemsPanel}" />
                    </Border>
                    <TextBlock Text="{TemplateBinding Hint}" FontSize="12.5" Foreground="{DynamicResource Helix.TextMuted}" TextWrapping="Wrap"
                               IsVisible="{TemplateBinding Hint, Converter={x:Static StringConverters.IsNotNullOrEmpty}}" />
                </StackPanel>
            </ControlTemplate>
        </Setter>
        <Style Selector="^[Orientation=Horizontal]">
            <Setter Property="ItemsPanel"><ItemsPanelTemplate><WrapPanel ItemSpacing="18" LineSpacing="10" /></ItemsPanelTemplate></Setter>
        </Style>
        <Style Selector="^[Appearance=Cards]">
            <Setter Property="ItemsPanel"><ItemsPanelTemplate><UniformGrid Columns="3" ColumnSpacing="10" RowSpacing="10" /></ItemsPanelTemplate></Setter>
        </Style>
        <Style Selector="^[Appearance=Cards] /template/ Border#Track"><Setter Property="HorizontalAlignment" Value="Stretch" /></Style>
        <Style Selector="^[Appearance=Segmented]">
            <Setter Property="ItemsPanel"><ItemsPanelTemplate><StackPanel Orientation="Horizontal" Spacing="2" /></ItemsPanelTemplate></Setter>
        </Style>
        <Style Selector="^[Appearance=Segmented] /template/ Border#Track">
            <Setter Property="Background" Value="{DynamicResource Helix.Surface2}" />
            <Setter Property="CornerRadius" Value="9" />
            <Setter Property="Padding" Value="3" />
        </Style>
    </ControlTheme>
''')

    # Slider
    out.append('''    <ControlTheme x:Key="HelixSliderFill" TargetType="RepeatButton">
        <Setter Property="Focusable" Value="False" />
        <Setter Property="Template">
            <ControlTemplate>
                <Border Height="6" CornerRadius="3" VerticalAlignment="Center" Background="{ReflectionBinding Accent, RelativeSource={RelativeSource AncestorType=h:HelixSlider}}" />
            </ControlTemplate>
        </Setter>
    </ControlTheme>
    <ControlTheme x:Key="HelixSliderRest" TargetType="RepeatButton">
        <Setter Property="Focusable" Value="False" />
        <Setter Property="Template">
            <ControlTemplate><Border Background="Transparent" /></ControlTemplate>
        </Setter>
    </ControlTheme>
    <ControlTheme x:Key="HelixSliderThumb" TargetType="Thumb">
        <Setter Property="Width" Value="18" />
        <Setter Property="Height" Value="18" />
        <Setter Property="Cursor" Value="Hand" />
        <Setter Property="Template">
            <ControlTemplate>
                <Border CornerRadius="9" Background="White" BorderThickness="2" BoxShadow="0 1 3 0 #33000000"
                        BorderBrush="{ReflectionBinding Accent, RelativeSource={RelativeSource AncestorType=h:HelixSlider}}" />
            </ControlTemplate>
        </Setter>
    </ControlTheme>

    <ControlTheme x:Key="{x:Type h:HelixSlider}" TargetType="h:HelixSlider">
        <Setter Property="Accent" Value="{DynamicResource Helix.Primary}" />
        <Setter Property="TrackBrush" Value="{DynamicResource Helix.Surface3}" />
        <Setter Property="FontFamily" Value="{DynamicResource Helix.FontFamily}" />
        <Setter Property="Foreground" Value="{DynamicResource Helix.Text}" />
        <Setter Property="Template">
            <ControlTemplate>
                <StackPanel Spacing="6">
                    <DockPanel IsVisible="{TemplateBinding Label, Converter={x:Static StringConverters.IsNotNullOrEmpty}}">
                        <TextBlock DockPanel.Dock="Right" Text="{TemplateBinding ValueText}" FontSize="13" FontWeight="SemiBold" IsVisible="{TemplateBinding ShowValue}" />
                        <TextBlock Text="{TemplateBinding Label}" FontSize="13" FontWeight="SemiBold" />
                    </DockPanel>
                    <Panel Height="22">
                        <Border Height="6" CornerRadius="3" Background="{TemplateBinding TrackBrush}" VerticalAlignment="Center" Margin="0" />
                        <Track Name="PART_Track" Orientation="Horizontal" Minimum="{TemplateBinding Minimum}" Maximum="{TemplateBinding Maximum}"
                               Value="{TemplateBinding Value, Mode=TwoWay}">
                            <Track.DecreaseButton><RepeatButton Name="PART_DecreaseButton" Theme="{StaticResource HelixSliderFill}" /></Track.DecreaseButton>
                            <Track.IncreaseButton><RepeatButton Name="PART_IncreaseButton" Theme="{StaticResource HelixSliderRest}" /></Track.IncreaseButton>
                            <Thumb Theme="{StaticResource HelixSliderThumb}" />
                        </Track>
                    </Panel>
                </StackPanel>
            </ControlTemplate>
        </Setter>
''' + "\n".join(f'''        <Style Selector="^[Tone={t}]"><Setter Property="Accent" Value="{{DynamicResource Helix.{t}}}" /></Style>''' for t in ["Neutral","Success","Warning","Danger","Info"]) + '''
        <Style Selector="^:disabled"><Setter Property="Opacity" Value="0.5" /></Style>
    </ControlTheme>

    <ControlTheme x:Key="{x:Type h:HelixRating}" TargetType="h:HelixRating">
        <Setter Property="Accent" Value="{DynamicResource Helix.Warning}" />
        <Setter Property="Focusable" Value="True" />
        <Setter Property="FontFamily" Value="{DynamicResource Helix.FontFamily}" />
        <Setter Property="Foreground" Value="{DynamicResource Helix.TextMuted}" />
        <Setter Property="Template">
            <ControlTemplate>
                <StackPanel Orientation="Horizontal" Spacing="10">
                    <StackPanel Name="PART_Icons" Orientation="Horizontal" Spacing="3" VerticalAlignment="Center" />
                    <TextBlock Text="{TemplateBinding Caption}" FontSize="13" VerticalAlignment="Center"
                               IsVisible="{TemplateBinding Caption, Converter={x:Static StringConverters.IsNotNullOrEmpty}}" />
                </StackPanel>
            </ControlTemplate>
        </Setter>
''' + "\n".join(f'''        <Style Selector="^[Tone={t}]"><Setter Property="Accent" Value="{{DynamicResource Helix.{t}}}" /></Style>''' for t in ["Neutral","Success","Danger","Info"]) + '''
    </ControlTheme>
''')
    return "\n".join(out)
