;;;===========================================================
;;; LỆNH: DELETETEXTANDLEADER
;;; Xoá Text, Leader, Leader Note (hoặc kết hợp)
;;; Tùy chọn: xoá tất cả / theo vùng / theo layer
;;;===========================================================

(vl-load-com)

(defun c:DELETETEXTANDLEADER ( / choice typeStr mode layerFilter filterList ss count)
  (prompt "\nLỆNH DELETETEXTANDLEADER - Xoá Text / Leader / Leader Note / Kết hợp.")

  ;; --- Bảng chọn loại cần xoá ---
  (initget 1 "1 2 3 4")
  (setq choice
    (getkword
      "\nChọn loại cần xoá [1:Text / 2:Leader / 3:LeaderNote / 4:Tất cả] <1>: "
    )
  )
  (if (not choice) (setq choice "1"))

  ;; Xác định loại đối tượng
  (setq typeStr
    (cond
      ((= choice "1") "TEXT,MTEXT")
      ((= choice "2") "LEADER")
      ((= choice "3") "MULTILEADER")
      ((= choice "4") "TEXT,MTEXT,LEADER,MULTILEADER")
    )
  )

  ;; --- Chọn chế độ ---
  (initget 1 "1 2")
  (setq mode (getkword "\n[1:Xoá tất cả / 2:Chọn vùng] <1>: "))
  (if (not mode) (setq mode "1"))

  ;; --- Lọc theo layer ---
  (setq layerFilter (getstring T "\nNhập tên Layer muốn xoá (Enter = tất cả): "))
  (setq filterList (list (cons 0 typeStr)))
  (if (and layerFilter (/= layerFilter ""))
    (setq filterList (append filterList (list (cons 8 layerFilter))))
  )

  ;; --- Chọn đối tượng ---
  (cond
    ((= mode "1") (setq ss (ssget "_X" filterList)))
    ((= mode "2")
      (prompt "\nChọn vùng cần xoá:")
      (setq ss (ssget filterList))
    )
  )

  ;; --- Xoá ---
  (if (and ss (> (sslength ss) 0))
    (progn
      (setq count (sslength ss))
      (command "_.ERASE" ss "")
      (prompt (strcat "\n→ Đã xoá " (itoa count) " đối tượng."))
    )
    (prompt "\nKhông tìm thấy đối tượng cần xoá.")
  )

  (princ)
)

(princ "\nGõ DELETETEXTANDLEADER để chạy lệnh xoá Text / Leader / Leader Note.")
(princ)
