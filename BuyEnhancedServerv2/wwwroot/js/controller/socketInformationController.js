myApp.controller("socketInformationController",
function($scope,Factory,$rootScope,$routeParams){
    $rootScope.loading = true;

    let endpoint = $routeParams.socketEndpoint;

    $scope.returnButtonLinkValue = endpoint == "trader"? "#!/trader":"#!/network"; 

    let displayTable = document.getElementById("displayTable");

    function htmlEscape(str) {
        return str.toString()
            .replace(/&/g, '&amp;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#39;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;');
    }

    let origin = "localhost:5218";

    let socket = new WebSocket("ws://" + origin + "/" + endpoint);

    socket.onopen = function () {
        displayTable.innerHTML = "<tr>"+"Connection opened"+"</tr>";
    };

    socket.onclose = function () {
        if(endpoint == "trader"){
            Factory.closeTraderSocket().then(
                (data)=>{
                    if(data.data.retCode != 0){
                        alert(data.data.retMessage);
                    }
                }
            );
        }
    };

    //socket.onerror = updateState;
    socket.onmessage = (event)=>{
        displayTable.innerHTML += "<tr>"+ event.data +"</tr>";
    };

    $scope.onClickButtonEventHandler = function () {
        if (!socket || socket.readyState !== WebSocket.OPEN) {
            alert("socket not connected");
        }
        socket.close(1000, "Closing from client");
    };

	$rootScope.loading = false;
}
);