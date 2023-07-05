class ClientService {
    onEvent(action, data){
        let customEvent = new CustomEvent("onClientService" + action, { detail: { data: data }});
        document.dispatchEvent(customEvent);
    }       
}

var clientServiceInstance = new ClientService();