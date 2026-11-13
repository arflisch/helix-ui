"""Templates of the timeline, kanban, sortable list and dashboard (appended to Controls.axaml)."""

def tile_button(command, icon, text, visible=None):
    vis = f' IsVisible="{visible}"' if visible else ""
    return f'''<Button Theme="{{StaticResource HelixIconButton}}" Command="{{ReflectionBinding {command}, RelativeSource={{RelativeSource TemplatedParent}}}}" ToolTip.Tip="{{x:Static h:HelixDashboardTile.{text}}}"{vis}>
                                        <h:HelixIcon Icon="{icon}" Size="14" />
                                    </Button>'''

def build():
    return '''
    <!-- ===================================================================== Timeline -->

    <ControlTheme x:Key="{x:Type h:HelixTimeline}" TargetType="h:HelixTimeline">
        <Setter Property="FontFamily" Value="{DynamicResource Helix.FontFamily}" />
        <Setter Property="Foreground" Value="{DynamicResource Helix.Text}" />
        <Setter Property="Background" Value="{DynamicResource Helix.Surface}" />
        <Setter Property="BorderBrush" Value="{DynamicResource Helix.Border}" />
        <Setter Property="BorderThickness" Value="1" />
        <Setter Property="CornerRadius" Value="14" />
        <Setter Property="Template">
            <ControlTemplate>
                <Border Background="{TemplateBinding Background}" BorderBrush="{TemplateBinding BorderBrush}" BorderThickness="{TemplateBinding BorderThickness}"
                        CornerRadius="{TemplateBinding CornerRadius}" ClipToBounds="True">
                    <DockPanel>
                        <DockPanel DockPanel.Dock="Top" Margin="16,12,12,8">
                            <Button Name="PART_Toggle" DockPanel.Dock="Right" Theme="{StaticResource HelixIconButton}" VerticalAlignment="Top"
                                    IsVisible="{TemplateBinding IsCollapsible}" ToolTip.Tip="{x:Static h:HelixTimeline.ToggleText}">
                                <h:HelixIcon Name="Chevron" Icon="chevron-down" Size="16" />
                            </Button>
                            <ContentPresenter DockPanel.Dock="Right" Content="{TemplateBinding HeaderContent}" VerticalAlignment="Center" Margin="8,0" />
                            <StackPanel Spacing="8">
                                <TextBlock Text="{TemplateBinding Title}" FontSize="15" FontWeight="SemiBold"
                                           IsVisible="{TemplateBinding Title, Converter={x:Static StringConverters.IsNotNullOrEmpty}}" />
                                <WrapPanel Name="PART_Legend" />
                            </StackPanel>
                        </DockPanel>
                        <ScrollViewer Name="PART_Scroll" HorizontalScrollBarVisibility="Auto" VerticalScrollBarVisibility="Disabled" Margin="0,0,0,8">
                            <Canvas Name="PART_Canvas" ClipToBounds="True" />
                        </ScrollViewer>
                    </DockPanel>
                </Border>
            </ControlTemplate>
        </Setter>
        <Style Selector="^[IsCollapsed=True] /template/ ScrollViewer#PART_Scroll"><Setter Property="IsVisible" Value="False" /></Style>
        <Style Selector="^[IsCollapsed=True] /template/ h|HelixIcon#Chevron"><Setter Property="RenderTransform" Value="rotate(-90deg)" /></Style>
    </ControlTheme>

    <!-- ===================================================================== Kanban -->

    <ControlTheme x:Key="{x:Type h:HelixKanban}" TargetType="h:HelixKanban">
        <Setter Property="FontFamily" Value="{DynamicResource Helix.FontFamily}" />
        <Setter Property="FontSize" Value="{DynamicResource Helix.FontSize}" />
        <Setter Property="Foreground" Value="{DynamicResource Helix.Text}" />
        <Setter Property="Template">
            <ControlTemplate>
                <Panel>
                    <ScrollViewer HorizontalScrollBarVisibility="Auto" VerticalScrollBarVisibility="Disabled">
                        <StackPanel Name="PART_Columns" Orientation="Horizontal" />
                    </ScrollViewer>
                    <Canvas Name="PART_Overlay" IsHitTestVisible="False" />
                </Panel>
            </ControlTemplate>
        </Setter>
    </ControlTheme>

    <!-- ===================================================================== Sortable list -->

    <ControlTheme x:Key="HelixSortableItem" TargetType="ListBoxItem">
        <Setter Property="Background" Value="{DynamicResource Helix.Surface}" />
        <Setter Property="BorderBrush" Value="{DynamicResource Helix.Border}" />
        <Setter Property="Foreground" Value="{DynamicResource Helix.Text}" />
        <Setter Property="Padding" Value="10,9" />
        <Setter Property="Margin" Value="0,0,0,6" />
        <Setter Property="Template">
            <ControlTemplate>
                <Border Background="{TemplateBinding Background}" BorderBrush="{TemplateBinding BorderBrush}" BorderThickness="1" CornerRadius="10" Padding="{TemplateBinding Padding}">
                    <DockPanel>
                        <h:HelixIcon Name="Grip" DockPanel.Dock="Left" Icon="grip" Size="16" Foreground="{DynamicResource Helix.TextSubtle}" Margin="0,0,10,0" VerticalAlignment="Center"
                                     Cursor="SizeAll" IsVisible="{ReflectionBinding ShowHandle, RelativeSource={RelativeSource AncestorType=h:HelixSortableList}}" />
                        <ContentPresenter Name="PART_ContentPresenter" Content="{TemplateBinding Content}" ContentTemplate="{TemplateBinding ContentTemplate}" VerticalAlignment="Center" />
                    </DockPanel>
                </Border>
            </ControlTemplate>
        </Setter>
        <Style Selector="^:pointerover"><Setter Property="BorderBrush" Value="{DynamicResource Helix.BorderStrong}" /></Style>
        <Style Selector="^:selected">
            <Setter Property="BorderBrush" Value="{DynamicResource Helix.Primary}" />
            <Setter Property="Background" Value="{DynamicResource Helix.PrimarySoft}" />
        </Style>
    </ControlTheme>

    <ControlTheme x:Key="{x:Type h:HelixSortableList}" TargetType="h:HelixSortableList">
        <Setter Property="FontFamily" Value="{DynamicResource Helix.FontFamily}" />
        <Setter Property="FontSize" Value="{DynamicResource Helix.FontSize}" />
        <Setter Property="Background" Value="Transparent" />
        <Setter Property="ItemContainerTheme" Value="{StaticResource HelixSortableItem}" />
        <Setter Property="ItemsPanel"><ItemsPanelTemplate><StackPanel /></ItemsPanelTemplate></Setter>
        <Setter Property="Template">
            <ControlTemplate>
                <ItemsPresenter Name="PART_ItemsPresenter" ItemsPanel="{TemplateBinding ItemsPanel}" />
            </ControlTemplate>
        </Setter>
    </ControlTheme>

    <!-- ===================================================================== Dashboard -->

    <ControlTheme x:Key="{x:Type h:HelixDashboard}" TargetType="h:HelixDashboard">
        <Setter Property="FontFamily" Value="{DynamicResource Helix.FontFamily}" />
        <Setter Property="FontSize" Value="{DynamicResource Helix.FontSize}" />
        <Setter Property="Foreground" Value="{DynamicResource Helix.Text}" />
        <Setter Property="ItemsPanel"><ItemsPanelTemplate><h:HelixDashboardPanel /></ItemsPanelTemplate></Setter>
        <Setter Property="Template">
            <ControlTemplate>
                <StackPanel Spacing="14">
                    <DockPanel IsVisible="{TemplateBinding ShowToolbar}">
                        <ToggleButton Name="Edit" DockPanel.Dock="Right" Theme="{StaticResource HelixBareToggle}" IsChecked="{TemplateBinding IsEditing, Mode=TwoWay}" VerticalAlignment="Center">
                            <Border Name="EditChrome" CornerRadius="8" BorderThickness="1" BorderBrush="{DynamicResource Helix.BorderStrong}" Background="{DynamicResource Helix.Surface}" Padding="12,6">
                                <StackPanel Orientation="Horizontal" Spacing="6">
                                    <h:HelixIcon Name="EditIcon" Icon="layout" Size="15" VerticalAlignment="Center" />
                                    <Panel>
                                        <TextBlock Name="EditText" Text="{x:Static h:HelixDashboard.EditText}" FontSize="13" FontWeight="SemiBold" />
                                        <TextBlock Name="DoneText" Text="{x:Static h:HelixDashboard.DoneText}" FontSize="13" FontWeight="SemiBold" IsVisible="False" />
                                    </Panel>
                                </StackPanel>
                            </Border>
                        </ToggleButton>
                        <ContentPresenter DockPanel.Dock="Right" Content="{TemplateBinding Toolbar}" VerticalAlignment="Center" Margin="0,0,10,0" />
                        <TextBlock Text="{TemplateBinding Title}" FontSize="17" FontWeight="SemiBold" VerticalAlignment="Center" />
                    </DockPanel>
                    <StackPanel Name="Hidden" Orientation="Horizontal" Spacing="8" IsVisible="False">
                        <TextBlock Text="{x:Static h:HelixDashboard.HiddenText}" FontSize="12.5" Foreground="{DynamicResource Helix.TextMuted}" VerticalAlignment="Center" />
                        <ItemsControl ItemsSource="{TemplateBinding HiddenChips}">
                            <ItemsControl.ItemsPanel><ItemsPanelTemplate><WrapPanel ItemSpacing="6" /></ItemsPanelTemplate></ItemsControl.ItemsPanel>
                            <ItemsControl.ItemTemplate>
                                <DataTemplate x:DataType="h:HelixHiddenTile">
                                    <h:HelixButton Content="{Binding Title}" Icon="plus" Variant="Soft" Size="Sm"
                                                   Command="{ReflectionBinding RestoreCommand, RelativeSource={RelativeSource AncestorType=h:HelixDashboard}}" CommandParameter="{Binding}" />
                                </DataTemplate>
                            </ItemsControl.ItemTemplate>
                        </ItemsControl>
                    </StackPanel>
                    <Panel>
                        <ItemsPresenter Name="PART_ItemsPresenter" ItemsPanel="{TemplateBinding ItemsPanel}" />
                        <Canvas Name="PART_MarkerLayer" IsHitTestVisible="False" />
                    </Panel>
                </StackPanel>
            </ControlTemplate>
        </Setter>
        <Style Selector="^:editing /template/ Border#EditChrome">
            <Setter Property="Background" Value="{DynamicResource Helix.Primary}" />
            <Setter Property="BorderBrush" Value="{DynamicResource Helix.Primary}" />
            <Setter Property="TextElement.Foreground" Value="{DynamicResource Helix.OnPrimary}" />
        </Style>
        <Style Selector="^:editing /template/ TextBlock#EditText"><Setter Property="IsVisible" Value="False" /></Style>
        <Style Selector="^:editing /template/ TextBlock#DoneText"><Setter Property="IsVisible" Value="True" /></Style>
        <Style Selector="^:editing:has-hidden /template/ StackPanel#Hidden"><Setter Property="IsVisible" Value="True" /></Style>
    </ControlTheme>

    <ControlTheme x:Key="{x:Type h:HelixDashboardTile}" TargetType="h:HelixDashboardTile">
        <Setter Property="Background" Value="{DynamicResource Helix.Surface}" />
        <Setter Property="BorderBrush" Value="{DynamicResource Helix.Border}" />
        <Setter Property="BorderThickness" Value="1" />
        <Setter Property="CornerRadius" Value="14" />
        <Setter Property="Padding" Value="18,4,18,18" />
        <Setter Property="Foreground" Value="{DynamicResource Helix.Text}" />
        <Setter Property="Template">
            <ControlTemplate>
                <Border Name="Frame" Background="{TemplateBinding Background}" BorderBrush="{TemplateBinding BorderBrush}" BorderThickness="{TemplateBinding BorderThickness}"
                        CornerRadius="{TemplateBinding CornerRadius}">
                    <DockPanel>
                        <DockPanel Name="PART_Handle" DockPanel.Dock="Top" Background="Transparent" Margin="18,14,12,8">
                            <StackPanel Name="Tools" DockPanel.Dock="Right" Orientation="Horizontal" Spacing="2" IsVisible="False">
                                ''' + tile_button("MoveBeforeCommand", "chevron-left", "MoveBeforeText") + '''
                                ''' + tile_button("MoveAfterCommand", "chevron-right", "MoveAfterText") + '''
                                ''' + tile_button("NarrowerCommand", "minus", "NarrowerText") + '''
                                <TextBlock Text="{TemplateBinding SpanText}" FontSize="12" Foreground="{DynamicResource Helix.TextMuted}" VerticalAlignment="Center" Margin="2,0" />
                                ''' + tile_button("WiderCommand", "plus", "WiderText") + '''
                                ''' + tile_button("HideCommand", "eye-off", "HideText", "{TemplateBinding CanHide}") + '''
                            </StackPanel>
                            <ContentPresenter DockPanel.Dock="Right" Content="{TemplateBinding Actions}" VerticalAlignment="Center" />
                            <StackPanel Orientation="Horizontal" Spacing="8">
                                <h:HelixIcon Name="Grip" Icon="grip" Size="14" Foreground="{DynamicResource Helix.TextSubtle}" VerticalAlignment="Center" IsVisible="False" />
                                <TextBlock Text="{TemplateBinding Title}" FontSize="14" FontWeight="SemiBold" VerticalAlignment="Center" />
                            </StackPanel>
                        </DockPanel>
                        <ContentPresenter Name="PART_ContentPresenter" Content="{TemplateBinding Content}" ContentTemplate="{TemplateBinding ContentTemplate}" Padding="{TemplateBinding Padding}" />
                    </DockPanel>
                </Border>
            </ControlTemplate>
        </Setter>
        <Style Selector="^[IsPlain=True] /template/ Border#Frame">
            <Setter Property="Background" Value="Transparent" />
            <Setter Property="BorderThickness" Value="0" />
        </Style>
        <Style Selector="^[IsPlain=True]"><Setter Property="Padding" Value="0" /></Style>
        <Style Selector="^[IsPlain=True]:not(:editing) /template/ DockPanel#PART_Handle"><Setter Property="IsVisible" Value="False" /></Style>
        <Style Selector="^:editing /template/ StackPanel#Tools"><Setter Property="IsVisible" Value="True" /></Style>
        <Style Selector="^:editing /template/ h|HelixIcon#Grip"><Setter Property="IsVisible" Value="True" /></Style>
        <Style Selector="^:editing /template/ DockPanel#PART_Handle"><Setter Property="Cursor" Value="SizeAll" /></Style>
        <Style Selector="^:editing /template/ Border#Frame">
            <Setter Property="BorderBrush" Value="{DynamicResource Helix.BorderStrong}" />
            <Setter Property="BorderThickness" Value="1" />
        </Style>
        <Style Selector="^:dragging"><Setter Property="Opacity" Value="0.55" /></Style>
        <Style Selector="^:dragging /template/ Border#Frame"><Setter Property="BorderBrush" Value="{DynamicResource Helix.Primary}" /></Style>
    </ControlTheme>
'''
