class ClientService {
    onEvent(action, data){
        console.log('onevent', action, data);
        let customEvent = new CustomEvent("onClientService" + action, { detail: { data: data }});
        document.dispatchEvent(customEvent);
    }       
}

var clientServiceInstance = new ClientService();