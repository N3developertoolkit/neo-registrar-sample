$path = "./Directory.Build.props"
$xml = [xml](gc $path)
$xml.Project.PropertyGroup.NeoTestVersion = gcb
set-content $path $xml.OuterXml