"""Templates of the details toggle, signed value and field wrapper (appended to Controls.axaml)."""

def build():
    return '''
    <!-- ===================================================================== Grid columns, field -->

    <ControlTheme x:Key="{x:Type h:HelixDetailsToggle}" TargetType="h:HelixDetailsToggle">
        <Setter Property="Width" Value="26" />
        <Setter Property="Height" Value="26" />
        <Setter Property="Background" Value="Transparent" />
        <Setter Property="Cursor" Value="Hand" />
        <Setter Property="HorizontalAlignment" Value="Center" />
        <Setter Property="VerticalAlignment" Value="Center" />
        <Setter Property="Template">
            <ControlTemplate>
                <Border Background="{TemplateBinding Background}" CornerRadius="6">
                    <h:HelixIcon Name="Chevron" Icon="chevron-right" Size="15" Foreground="{DynamicResource Helix.TextMuted}" HorizontalAlignment="Center" VerticalAlignment="Center">
                        <h:HelixIcon.Transitions><Transitions><TransformOperationsTransition Property="RenderTransform" Duration="0:0:0.15" /></Transitions></h:HelixIcon.Transitions>
                    </h:HelixIcon>
                </Border>
            </ControlTemplate>
        </Setter>
        <Style Selector="^:pointerover"><Setter Property="Background" Value="{DynamicResource Helix.Surface3}" /></Style>
        <Style Selector="^:checked /template/ h|HelixIcon#Chevron"><Setter Property="RenderTransform" Value="rotate(90deg)" /><Setter Property="Foreground" Value="{DynamicResource Helix.Primary}" /></Style>
    </ControlTheme>

    <ControlTheme x:Key="{x:Type h:HelixSignedValue}" TargetType="h:HelixSignedValue">
        <Setter Property="Foreground" Value="{DynamicResource Helix.TextMuted}" />
        <Setter Property="FontWeight" Value="SemiBold" />
        <Setter Property="VerticalAlignment" Value="Center" />
        <Setter Property="Template">
            <ControlTemplate>
                <StackPanel Orientation="Horizontal" Spacing="4">
                    <h:HelixIcon Name="Arrow" Size="13" StrokeWidth="2.4" VerticalAlignment="Center" IsVisible="False" />
                    <TextBlock Text="{TemplateBinding Text}" VerticalAlignment="Center" FontFeatures="tnum" />
                </StackPanel>
            </ControlTemplate>
        </Setter>
        <Style Selector="^:up /template/ h|HelixIcon#Arrow"><Setter Property="Icon" Value="arrow-up" /><Setter Property="IsVisible" Value="True" /></Style>
        <Style Selector="^:down /template/ h|HelixIcon#Arrow"><Setter Property="Icon" Value="arrow-down" /><Setter Property="IsVisible" Value="True" /></Style>
        <Style Selector="^:good"><Setter Property="Foreground" Value="{DynamicResource Helix.Success}" /></Style>
        <Style Selector="^:bad"><Setter Property="Foreground" Value="{DynamicResource Helix.Danger}" /></Style>
    </ControlTheme>

    <ControlTheme x:Key="{x:Type h:HelixField}" TargetType="h:HelixField">
        <Setter Property="FontFamily" Value="{DynamicResource Helix.FontFamily}" />
        <Setter Property="FontSize" Value="{DynamicResource Helix.FontSize}" />
        <Setter Property="Foreground" Value="{DynamicResource Helix.Text}" />
        <Setter Property="Template">
            <ControlTemplate>
                <StackPanel Spacing="6">
                    <StackPanel Orientation="Horizontal" Spacing="3" IsVisible="{TemplateBinding Label, Converter={x:Static StringConverters.IsNotNullOrEmpty}}">
                        <TextBlock Text="{TemplateBinding Label}" FontSize="13" FontWeight="SemiBold" />
                        <TextBlock Text="*" FontSize="13" FontWeight="SemiBold" Foreground="{DynamicResource Helix.Danger}" IsVisible="{TemplateBinding IsRequired}" />
                    </StackPanel>
                    <ContentPresenter Name="PART_ContentPresenter" Content="{TemplateBinding Content}" ContentTemplate="{TemplateBinding ContentTemplate}" />
                    <TextBlock Name="Message" Text="{TemplateBinding Message}" FontSize="12.5" TextWrapping="Wrap" Foreground="{DynamicResource Helix.TextMuted}"
                               IsVisible="{TemplateBinding Message, Converter={x:Static StringConverters.IsNotNullOrEmpty}}" />
                </StackPanel>
            </ControlTemplate>
        </Setter>
        <Style Selector="^:error /template/ TextBlock#Message"><Setter Property="Foreground" Value="{DynamicResource Helix.Danger}" /></Style>
    </ControlTheme>
'''
