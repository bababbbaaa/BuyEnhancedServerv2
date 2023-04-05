myApp.controller("traderController",
function($scope,Factory,$rootScope){
    $rootScope.loading = true;

    function updateTraderList(){
        Factory.getSubscriptionList().then(
            (data)=>{
                let traderList = data.data.result.list;
                for(let i in traderList){

                    traderList[i].index = i;

                    switch(traderList[i].state){
                        case "stopped": 
                            traderList[i].actionButtonText = "Start"; 
                            break;
                        case "running":
                            traderList[i].actionButtonText = "Soft Stop"; 
                            break;
                        case "soft stop":
                            traderList[i].actionButtonText = "Brutal Stop"; 
                            break;
                        default:
                            traderList[i].actionButtonText = "Error"; 
                            break;
                    }
                }

                console.log(traderList);

                $scope.traderList = traderList;
            }
        );
    }

    $scope.onClickActionButtonEventHandler = (index)=>{

        switch($scope.traderList[index].state){
            case "stopped" : 
                Factory.launchSubscription($scope.traderList[index].encryptedUid).then(
                    (data)=>{
                        if(data.retCode != 0){
                            alert(data.retMessage);
                        }
                        console.log(data);
                        updateTraderList();
                    }
                );
                break;
            case "running" : 
                Factory.softStop($scope.traderList[index].encryptedUid).then(
                    (data)=>{
                        if(data.retCode != 0){
                            alert(data.retMessage);
                        }
                        console.log(data);
                        updateTraderList();
                    }
                );
                break;
            case "soft stop" : 
            console.log("bonjour")
                Factory.brutalStop($scope.traderList[index].encryptedUid).then(
                    (data)=>{
                        if(data.retCode != 0){
                            alert(data.retMessage);
                        }
                        console.log(data);
                        updateTraderList();
                    }
                );
                break;
            default: 
                console.log("Error");
                break;
        }
    }

    $scope.onClickDeleteButtonEventHandler = (index)=> {
        Factory.deleteTrader($scope.traderList[index].encryptedUid).then(
            (data)=>{
                if(data.retCode != 0){
                    alert(data.retMessage);
                }
                console.log(data);
                updateTraderList();
            }
        );
    }

    $scope.openTraderSocket = (index) => {
        Factory.openTraderSocket($scope.traderList[index].encryptedUid).then(
            (data)=>{
                if(data.retCode != 0){
                    alert(data.retMessage);
                }
                console.log(data);
                updateTraderList();
            }
        );
    }

    updateTraderList();

	$rootScope.loading = false;
}
);