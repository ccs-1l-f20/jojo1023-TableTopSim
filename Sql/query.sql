Select uspaddgame('test', 10, 10, 1, 1, 'sd')
Select uspaddgame('test2', 10, 10, 1, 2, 'sd')
Select * From uspcreateplayerandroom(2, 'test')
Select * From uspcreateplayerinroom('test2', 19)
Select * From rooms
Select * From games

create or replace function uspcreateplayerinroom(playername varchar(100), roomid int)
returns table(playerid int, noroom bool)
as $$
declare
	pct int;
	pid int;
	gid int;
-- variable declaration
begin
	IF EXISTS
		(Select * From rooms Where rooms.roomid = $2 and roomopen = true)
	Then
		Select Count(*) into pct From players where players.roomid = $2;
		Select GameId into gid From rooms Where rooms.roomid = $2;
		If(pct + 1 <= (Select maxplayers From Games Where gameid = gid))
		Then
			Insert Into Players (Name, roomid) Values (playername, roomid);
			pid := currval('players_playerid_seq');
			return query Select pid, false;
		else
			return query Select Cast(Null as int), true;
		end if;
	else
		return query select Cast(Null as int), false;
	End if;
end; $$ language plpgsql


DROP Function uspcreateplayerandroom(integer,character varying)