class ClientService {
    onEvent(action, data){
        if(typeof(data) === 'string' && /[^[\s]+[{\[]/.test(data))
        {            
            try {
                data = JSON.parse(data);;
            }catch(err) {
                console.log('##### not json', data);
            }
        }
        let customEvent = new CustomEvent("onClientService" + action, { detail: { data: data }});
        document.dispatchEvent(customEvent);
    }       
}

var clientServiceInstance = new ClientService();