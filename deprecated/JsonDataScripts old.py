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
            outputRelated.append(paths[i])  # TODO: 可选是否保留 Related Path 的名称
    else:
        if attributes[i]["id"].startswith("_"):
            outputVertices.append(attributes[i])
        else:
            outputLocations.append(attributes[i])


pathCount = 0
verticeCount = 0

graph = {
    "Vertices": [],
    "Paths": [],
    "Locations": []
}

for i in outputVertices:
    graph["Vertices"].append({
        "Id": verticeCount,
        "Name": f"Vertice_{verticeCount}",  # TODO: Vertice 节点，Name字段可更改
        "X": float(i["cx"]),  # 这里事后debug一下，存储float值还是str值
        "Y": float(i["cy"]),
    })
    verticeCount += 1


def findVerticeId(vertices, point):
    for i in vertices:
        if i["X"] == point[0] and i["Y"] == point[1]:
            return i["Id"]


def getXY(point):
    result = str(point)[1:-2].split('+')
    return (float(result[0]), float(result[1]))


def getPathStart(path):
    return getXY(path.start)


def getPathEnd(path):
    return getXY(path.end)


for i in outputPaths:
    graph["Paths"].append({
        "Id": pathCount,
        "Name": f"Path_{pathCount}", # TODO: Path，Name字段可更改
        "Distance": i.length(),
        "StartVerticeId": findVerticeId(graph["Vertices"], getPathStart(i)),
        "EndVerticeId": findVerticeId(graph["Vertices"], getPathEnd(i)),
        "IsRelated": False,
    })
    pathCount += 1


for i in outputLocations:
    graph["Locations"].append({
        "Id": verticeCount,
        "Name": f"Vertice_{verticeCount}", # TODO: Location，Name字段可更改
        "Alias": i["id"],      
        "X": float(i["cx"]),  # 这里事后debug一下，存储float值还是str值
        "Y": float(i["cy"]),
        "LocatedVerticeIds": [],
        "LocatedPathIds": [],
    })
    verticeCount += 1


def findLocationAndVerticeId(locations, vertices, path):
    locationId = -1
    verticeId = -1
    if findVerticeId(locations, getPathStart(path)) == None:
        locationId = findVerticeId(locations, getPathEnd(path))
        verticeId = findVerticeId(vertices, getPathStart(path))
    else:
        locationId = findVerticeId(locations, getPathStart(path))
        verticeId = findVerticeId(vertices, getPathEnd(path))

    return locationId, verticeId

for i in outputRelated:
    locationId, verticeId = findLocationAndVerticeId(graph["Locations"], graph["Vertices"], i)
    graph["Paths"].append({
        "Id": pathCount,
        "Name": f"Path_{pathCount}",     # TODO: Related 路径，Name字段可更改
        "Distance": i.length(),
        "StartVerticeId": locationId,
        "EndVerticeId": verticeId,
        "IsRelated": True,
    })
    pathCount += 1


def getRelatedVerticesAndPaths(location):
    vertices = []
    paths = []
    for path in graph["Paths"]:
        if path["StartVerticeId"] == location["Id"] and path["IsRelated"] == True:
            vertices.append(path["EndVerticeId"])
            paths.append(path["Id"])

    return vertices, paths


for i in graph["Locations"]:
    locatedVertices, locatedPaths = getRelatedVerticesAndPaths(i)
    i["LocatedVerticeIds"] += locatedVertices
    i["LocatedPathIds"] += locatedPaths



# 这是魔法，性能不行，易读性差，但是很帅（

# for i in graph["Locations"]:
#     locatedVertices, locatedPaths = getRelatedVerticesAndPaths(i)
#     i["LocatedVerticeIds"].append([path["EndVerticeId"] for path in graph["Paths"] if path["StartVerticeId"] == i["Id"] and path["IsRelated"] == True])
#     i["LocatedPathIds"].append([path["Id"] for path in graph["Paths"] if path["StartVerticeId"] == i["Id"] and path["IsRelated"] == True])

DEBUG = True
if DEBUG:
    jsonString = json.dumps(graph, indent=2, sort_keys=True)
    file = open("graph.json", "w")
    file.write(jsonString)


"""---------------------------------------------------------------------------------------------------"""



def getPaths(method):
    result = []
    for i in range(len(outputPaths)):
        result.append(method(graph["Paths"][i]["Name"], outputPaths[i].d()))
    for i in range(len(outputRelated)):
        result.append(method(graph["Paths"][i + len(outputPaths)]["Name"], outputPaths[i].d()))

    # list[str]
    return result


def getVertices(method):
    result = []
    for vertice in graph["Vertices"]:
        result.append(
            method(vertice["Name"], vertice["X"], vertice["Y"], 1.5)) # 这里设置 vertice 节点的半径

    # list[str]
    return result


def getLocations(method):
    result = []
    for location in graph["Locations"]:
        result.append(
            method(location["Name"], location["X"], location["Y"]))

    # list[str]
    return result


"""---------------------------------------------------------------------------------------------------"""


def getXAMLPath(name, path):
    return f"""
<Path x:Name="{name}" Style="{{StaticResource Route}}">
    <Path.Data>
        <PathGeometry Figures="{path}" />
    </Path.Data>
</Path>"""


def getXAMLEllipse(name, x, y, r):

    # SVG 转 XAML 时需要将数值转化，只适用于无边框圆形
    return f"""
<Ellipse x:Name="{name}" Style="{{StaticResource WayPoint}}"
         Canvas.Left="{x - r}" Canvas.Top="{y - r}"
         Height="{r * 2}" Width="{r * 2}" />"""


def getXAMLLocation(name, x, y):
    return f"""
<Button x:Name="{name}" Style="{{StaticResource Location}}"
        Canvas.Left="{x}" Canvas.Top="{y}"
        Click="Location_OnClick"
        MouseDoubleClick="Location_OnMouseDoubleClick" />"""

EXPORT = True
if EXPORT:
    output = open("output.xaml", "w")
    output.writelines(getPaths(getXAMLPath))
    output.writelines(getVertices(getXAMLEllipse))
    output.writelines(getLocations(getXAMLLocation))
    output.close()

