from svgpathtools import svg2paths
import json

paths, attributes = svg2paths("SchoolMap.svg")

outputPaths = []
outputRelated = []
outputVertices = []
outputLocations = []

for i in range(len(paths)):
    if "r" not in attributes[i].keys():
        if "id" not in attributes[i].keys():
            outputPaths.append(paths[i])
        else:
            outputRelated.append((paths[i], attributes[i]["id"]))
    else:
        if attributes[i]["id"].startswith("_"):
            outputVertices.append(attributes[i])
        else:
            outputLocations.append(attributes[i])


def getPaths(method, **kwargs):
    result = []
    for path in outputPaths:
        result.append(method(path.d(), **kwargs))

    # list[str]
    return result


def getVertices(method, **kwargs):
    result = []
    for vertice in outputVertices:
        result.append(
            method(vertice["cx"], vertice["cy"], vertice["r"], **kwargs))

    # list[str]
    return result


def getLocations(method, **kwargs):
    result = []
    for location in outputLocations:
        result.append(
            method(location["cx"], location["cy"], location["r"], **kwargs))

    # list[str]
    return result


def getXAMLPath(path, **kwargs):
    return f"""
<Path Stroke="{kwargs["strokeColor" if "strokeColor" in kwargs.keys() else "Black"]}"
      StrokeThickness="{kwargs["strokeThickness"] if "strokeThickness" in kwargs.keys() else "3"}">
    <Path.Data>
        <PathGeometry Figures="{path}" />
    </Path.Data>
</Path>"""


def getXAMLEllipse(x, y, r, **kwargs):
    verticeColor = kwargs["verticeColor"] if "verticeColor" in kwargs.keys(
    ) else ""
    locationColor = kwargs["locationColor"] if "locationColor" in kwargs.keys(
    ) else ""
    # SVG 转 XAML 时需要将数值转化，只适用于无边框圆形
    return f"""
<Ellipse Fill="{verticeColor + locationColor}"
         Canvas.Left="{float(x) - float(r)}"
         Canvas.Top="{float(y) - float(r)}"
         Height="{float(r) * 2}"
         Width="{float(r) * 2}" />"""


DEBUG = False
if DEBUG:
    pass
else:
    output = open("output.xaml", "w")
    output.writelines(
        getPaths(getXAMLPath, strokeColor="Green", strokeThickness="3"))
    output.writelines(getVertices(getXAMLEllipse, verticeColor="Black"))
    output.writelines(getLocations(getXAMLEllipse, locationColor="Red"))
    output.close()

