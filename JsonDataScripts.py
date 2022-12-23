from svgpathtools import svg2paths
import json

paths, attributes = svg2paths("SchoolMap.svg")

outputPaths = []
outputVertices = []
outputLocations = []

ADDINFO = False
locationInfo = {
"宿舍楼3幢":                   "男生宿舍。其中702室居住着珊瑚宫的战士们。",
"宿舍楼B11":                   "男生宿舍。",
"菜鸟驿站":                    "学校的快递中心，负责收发所有快递。",
"东门":                        "在菜鸟驿站的东侧。快递和货物从这里进出，一般不对师生开放。",
"体育馆":                      "于2021年建成的新体育馆，有着干净漂亮的羽毛球场地。该馆承办过许多民间或者官方的赛事和活动。",
"旧操场":                      "包含排球场、篮球场和旧足球场。虽距离宿舍楼较远，在课余时间依然能见到许许多多挥洒汗水的身影。旁边的旧足球场已经废弃，杂草横生，是校园内有较好环境的自然去处。",
"宿舍楼A1":                    "女生宿舍。",
"教学楼C":                     "多媒体教学楼，最高5层。",
"图书馆":                      "全校最大的藏书室。有着众多小说和文学作品，同学们总能在这里找到自己的最爱。",
"第二行政楼":                  "神秘领域，具体信息暂时未公开。",
"宿舍楼D1":                    "男生宿舍。",
"实验楼A":                     "[待完善]XXX院的实验楼，有着众多先进的仪器和设备。",
"实验楼B":                     "计算机学院的实验楼，著名的紫金学院IT工作室“ZJIT”就坐落于其中。每年从这幢楼里走出来的“高手”数不胜数。",
"实验楼C":                     "电光学院实验楼，偶尔会承接其它学院的专业类课程。",
"紫金湖":                      "你在桥上看风景，看风景的人在楼上看你~。",
"实验楼D":                     "[待完善]XXX院的实验楼，很神秘。",
"西门":                        "学校平日里师生进出的主要通道。由于出门后外面就是超市、商场、水果店等店铺，很受同学们欢迎。",
"南大门":                      "学校的大门。新生入学和返校都在这里进出。学校最气派的紫金雕刻也在这里。",
"教学楼B":                     "理论课的主要教学楼。相信每位同学都在这里度过了难忘的高等数学课。",
"教学楼A":                     "英语教学楼。4楼是国际部的专属空间。",
"一食堂":                      "食堂内有一块大屏幕，可以看世界杯、LPL、TI等比赛。这个食堂除了贵没有缺点。",
"行政楼":                      "校领导驻扎地。",
"二食堂":                      "学校内最大的食堂，共三层。",
"大操场":                      "大操场，好大哎。",
}

for i in range(len(paths)):
    if "r" not in attributes[i].keys():
        outputPaths.append(paths[i])
    else:
        if attributes[i]["class"] == "cls-3":   # 如果改了节点的名称，这里要改为 data-name 的判断方式
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
        "Name": i["data-name"],  # TODO: Vertice 节点，Name字段可更改
        "X": float(i["cx"]),  # 这里事后debug一下，存储float值还是str值
        "Y": float(i["cy"]),
    })
    verticeCount += 1


def findVerticeId(source, point):
    for vertices in source:
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
        "StartVerticeId": -1,
        "EndVerticeId": -1,
        "Data": i.d(),
        "IsEnabled": True,
    })
    pathCount += 1


# findVerticeId([graph["Vertices"], graph["Locations"]], getPathStart(i))
# findVerticeId([graph["Vertices"], graph["Locations"]], getPathEnd(i))
for i in outputLocations:
    graph["Locations"].append({
        "Id": verticeCount,
        "Name": i["data-name"], 
        "Info": "" if ADDINFO else locationInfo[i["data-name"]],     
        "X": float(i["cx"]),  # 这里事后debug一下，存储float值还是str值
        "Y": float(i["cy"]),
        "LocatedVerticeIds": [],
        "LocatedPathIds": [],
    })
    verticeCount += 1

for i in range(len(graph["Paths"])):
    graph["Paths"][i]["StartVerticeId"] = findVerticeId([graph["Vertices"], graph["Locations"]], getPathStart(outputPaths[i]))
    graph["Paths"][i]["EndVerticeId"] = findVerticeId([graph["Vertices"], graph["Locations"]], getPathEnd(outputPaths[i]))



# def findLocationAndVerticeId(locations, vertices, path):
#     locationId = -1
#     verticeId = -1
#     if findVerticeId(locations, getPathStart(path)) == None:
#         locationId = findVerticeId(locations, getPathEnd(path))
#         verticeId = findVerticeId(vertices, getPathStart(path))
#     else:
#         locationId = findVerticeId(locations, getPathStart(path))
#         verticeId = findVerticeId(vertices, getPathEnd(path))

#     return locationId, verticeId

# for i in outputRelated:
#     locationId, verticeId = findLocationAndVerticeId(graph["Locations"], graph["Vertices"], i)
#     graph["Paths"].append({
#         "Id": pathCount,
#         "Name": f"Path_{pathCount}",     # TODO: Related 路径，Name字段可更改
#         "Distance": i.length(),
#         "StartVerticeId": locationId,
#         "EndVerticeId": verticeId,
#         "IsRelated": True,
#     })
#     pathCount += 1


def getRelatedVerticesAndPaths(location):
    vertices = []
    paths = []
    for path in graph["Paths"]:
        if path["StartVerticeId"] == location["Id"]:
            vertices.append(path["EndVerticeId"])
            paths.append(path["Id"])
        elif path["EndVerticeId"] == location["Id"]:
            vertices.append(path["StartVerticeId"])
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

DEBUG = not ADDINFO
if DEBUG:
    jsonString = json.dumps(graph, indent=2, sort_keys=True)
    file = open("""./SchoolNavigator/data/graph.json""", "w")
    file.write(jsonString)
    print("Success.")
else:
    for location in graph["Locations"]:
        print(f',"{location["Name"]}": \t\t\t""')

"""---------------------------------------------------------------------------------------------------"""

DEPRECATED = True
if DEPRECATED:

    """以下都是本用于输出 XAML 样式的脚本，现已停用"""


    def getPaths(method):
        result = []
        for i in range(len(outputPaths)):
            result.append(method(graph["Paths"][i]["Name"], outputPaths[i].d()))
        # for i in range(len(outputRelated)):
        #     result.append(method(graph["Paths"][i + len(outputPaths)]["Name"], outputPaths[i].d()))

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


EXPORT = False
if EXPORT:
    output = open("output.xaml", "w")
    output.writelines(getPaths(getXAMLPath))
    output.writelines(getVertices(getXAMLEllipse))
    output.writelines(getLocations(getXAMLLocation))
    output.close()

